using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Constants;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{

    // restricts access to this controller so only authenticated users can access it
    [Authorize]
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        // 2MB Image file limit.
        // TODO - Consider moving this to a configuration file or constant class for better maintainability.
        const long MAX_IMG_SIZE = 2 * 1024 * 1024;
        
        // Logger to track dashboard access and activity. 
        private readonly ILogger<DashboardController> _logger;

        // Dep. Injection for image upload, profile bio, and authentication services.
        private readonly IProfileImageService _imageService;

        private readonly IAuthenticationService _authService;

        private readonly IUserService _userService;

        private readonly INotificationService _notificationService;

        private readonly IRepository<Notification, ApplicationDbContext> _notificationRepo;

        private readonly IBadgeService _badgeService;
        private readonly IRepository<Badge, ApplicationDbContext> _badgeRepo; // For Badges page

        private readonly INotificationPreferenceService _notificationPreferenceService;

        // Constructor that injects the logger dependency
        public DashboardController(ILogger<DashboardController> logger,
            IProfileImageService imageService, IAuthenticationService authService,
            IBadgeService badgeService, INotificationService notificationService,
            IUserService userService, IRepository<Notification, ApplicationDbContext> notificationRepo,
            IRepository<Badge, ApplicationDbContext> badgeRepo,
            INotificationPreferenceService notificationPreferenceService)
        {
            _logger = logger;
            _imageService = imageService;
            _authService = authService;
            _badgeService = badgeService;
            _badgeRepo = badgeRepo;
            _notificationService = notificationService;
            _userService = userService;
            _notificationRepo = notificationRepo;
            _notificationPreferenceService = notificationPreferenceService;
        }

        // Displays the main dashboard page for authenticated users. 
        public async Task<IActionResult> Index([FromQuery] bool sighting_success = false,
            [FromQuery] int? points_earned = null)
        {
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);
            _logger.LogInformation("User {Email} accessed dashboard", User.Identity?.Name);

            var statusMsgHtml = "<p>You are successfully logged in. Time to explore our nature, together!</p>";
            if (sighting_success)
            {
                if (points_earned.HasValue && user != null && user.Sightings.Count > 0)
                {
                    statusMsgHtml =
                        $"<p class='fw-bold'>Congratulations! Your Sighting was uploaded successfully!</p>" +
                        $"<p class='text-success fw-bold'>You earned {points_earned.Value} points for this sighting!</p>";
                }
                else
                {
                    statusMsgHtml = "<p class='fw-bold'>Congratulations! Your Sighting was uploaded successfully!</p>";
                }
            }

            // Get the user device's local timezone cookie, default timezone is PST
            string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

            // Soft Update for Sighting Novice Badge ****************************
            var noviceBadgeId = BadgeId.SightingNoviceBadgeGUID;

            var badgeExists = await _badgeRepo.FindByIdAsync(noviceBadgeId);
            if (badgeExists != null)
            {
                int totalSightings = user!.Sightings.Count;
            
                // Check if the user already has a record for this badge
                var noviceRecord = user.UserBadges.FirstOrDefault(ub => ub.BadgeId == noviceBadgeId);
                bool isEarned = noviceRecord?.BadgeEarned.HasValue ?? false;

                if (!isEarned)
                {
                    // Case 1: They have enough sightings (5) to earn it immediately
                    if (totalSightings >= 5)
                    {
                        await _badgeService.AddBadge(user, noviceBadgeId, userTimeZoneId);
                    }
                    // Case 2: They have partial progress that isn't reflected in their UserBadge record
                    else if (totalSightings > (noviceRecord?.BadgeProgress ?? 0))
                    {
                        int currentProgress = noviceRecord?.BadgeProgress ?? 0;

                        // Calls UpdateBadge for each missing "step"
                        for (int i = currentProgress; i < totalSightings; i++)
                        {
                            await _badgeService.UpdateBadge(user, noviceBadgeId);
                        }
                    }
                }
            }

            // Get sorted Badges for display
            var sortedBadges = new List<UserBadge>();
            if (user != null)
            {
                var earnedBadges = user.UserBadges.Where(ub => ub.BadgeEarned.HasValue).ToList();
                sortedBadges = await _badgeService.SortBadgesByTime(earnedBadges);

                TimeZoneInfo userZone;
                try
                {
                    userZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
                }
                catch
                {
                    // Fallback for Windows environment or invalid IANA IDs
                    userZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                }

                // Convert the UTC DB BadgeEarned DateTimeOffsets to local time
                foreach (UserBadge ub in sortedBadges)
                {
                    if (ub.BadgeEarned.HasValue)
                    {
                        ub.BadgeEarned = TimeZoneInfo.ConvertTime(ub.BadgeEarned.Value, userZone);
                    }
                }
            }

            ViewData["UserTimeZone"] = userTimeZoneId;
            ViewData["SortedBadges"] = sortedBadges;
            ViewData["statusMsgHtml"] = statusMsgHtml;
            return View();
        }

        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadProfileImage(IFormFile? profilePicture)
        {
            // Clear and possible outstanding ModelState errors to ensure a clean slate for the view.
            // Doing this since we use custom modelErrors in the POST action, so ASP.NET's default
            // validation service won't clear our errors for us.
            ModelState.Clear();

            // Ensures not null and has content before processing. Logs a warning if the file is null or empty.
            if (profilePicture is { Length: > 0 })
            {
                var userEmail = User.Identity?.Name;

                // Reject the file if it's over 2MB
                if (profilePicture.Length > MAX_IMG_SIZE)
                {
                    _logger.LogWarning("Rejecting upload: File size {Size} exceeds 2MB limit.", profilePicture.Length);
                    ModelState.AddModelError(nameof(profilePicture), "File size exceeds the 2MB limit.");
                    return RedirectToAction("Settings");
                }

                // Reject if not an image file based on content type
                // (basic check, can be bypassed but serves as a first line of defense)
                // TODO - Consider implementing a more robust file type validation
                // (e.g., checking file signatures) for better security.
                if (!profilePicture.ContentType.StartsWith("image/"))
                {
                    _logger.LogWarning("Rejecting upload: Invalid content type {ContentType}.",
                        profilePicture.ContentType);
                    ModelState.AddModelError(nameof(profilePicture), "Invalid file type. Please upload an image.");
                    return RedirectToAction("Settings");
                }

                // Delegate to ProfileImageService
                var imageData = await _imageService.ConvertToBytesAsync(profilePicture);

                // Get the User object using the email from earlier
                var user = await _userService.GetUserByEmailAsync(userEmail ?? "");

                // Check for null before saving to DB
                if (userEmail != null && user != null && imageData is { Length: > 0 })
                {
                    // Save the actual bytes to the database via the service
                    await _userService.UpdateUserProfileImageAsync(userEmail, imageData, profilePicture.ContentType);
                    _logger.LogInformation("Profile image updated for user {Email}", userEmail);

                    // Get the user device's local timezone cookie, default timezone is PST
                     string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

                    // Add the Custom Profile Badge to the User object
                    await _badgeService.AddBadge(user, BadgeId.ProfileBadgeGUID, userTimeZoneId);
                }
            }
            else
            {
                _logger.LogWarning("Upload attempted with null or empty file.");
            }

            return RedirectToAction("Settings");
        }

        [HttpPost("UpdateBio")]
        public async Task<IActionResult> UpdateUserBio(string newBio)
        {
            var userEmail = User.Identity?.Name;

            // If no user is found, for whatever reason, this should return null
            var user = await _userService.GetUserByEmailAsync(userEmail ?? "");

            if (user != null)
            {
                // Delegate actual logic to the UserProfileService
                await _userService.UpdateUserBioAsync(user, newBio);
                _logger.LogInformation("Bio field updated for {Email}", userEmail);

                // If they are resetting their Bio back to the default,
                //                                 skip the badge update.
                if (!string.IsNullOrWhiteSpace(newBio))
                {
                    // Get the user device's local timezone cookie, default timezone is PST
                    string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

                    // Add the custom Bio Badge to the UserBadges list.
                    // Does not add the badge if the user already has a custom bio.
                    await _badgeService.AddBadge(user, BadgeId.CustomBioBadgeGUID, userTimeZoneId);
                }
            }

            return RedirectToAction("Settings");
        }

        [HttpGet("settings")]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost("UpdateDisplayName")]
        public async Task<IActionResult> UpdateDisplayName(string newDisplayName)
        {
            var userEmail = User.Identity?.Name;
            var user = await _userService.GetUserByEmailAsync(userEmail ?? "");

            if (user == null)
                return RedirectToAction("Settings");

            if (string.IsNullOrWhiteSpace(newDisplayName) || newDisplayName.Length < 2 || newDisplayName.Length > 50)
            {
                TempData["DisplayNameError"] = "Display name must be between 2 and 50 characters.";
                return RedirectToAction("Settings");
            }

            await _userService.UpdateDisplayNameAsync(user, newDisplayName);
            _logger.LogInformation("User {Email} updated display name to '{DisplayName}'", userEmail, newDisplayName);

            TempData["DisplayNameSuccess"] = "Display name updated successfully.";
            return RedirectToAction("Settings");
        }

        [HttpGet]
        [Route("/notifications")]
        public async Task<IActionResult> Notifications()
        {
            // Get the current user based on their claims principal. This is necessary to fetch their specific notifications.
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);

            // If for some reason we can't find the user (which shouldn't happen since this controller is protected by [Authorize]),
            // we return a 403 Forbidden response.
            if (user == null)
            {
                return Forbid();
            }

            var notifications = await _notificationService.GetAllNotificationsAsync(user);

            return View(notifications.ToList());
        }

        [HttpGet]
        [Route("/notifications/pending-count")]
        public async Task<IActionResult> GetPendingNotificationsCount()
        {
            // Get the current user based on their claims principal. This is necessary to fetch their specific notifications.
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);

            // If for some reason we can't find the user (which shouldn't happen since this controller is protected by [Authorize]),
            // we return a 403 Forbidden response.
            if (user == null)
            {
                return Forbid();
            }

            var count = await _notificationService.GetPendingNotificationsCountAsync(user);

            return Ok(count);
        }

        // This action handles updates to notifications, such as toggling the read/unread status.
        // It is designed to be called via AJAX from the frontend like an API endpoint.
        [HttpPut]
        [ValidateAntiForgeryToken]
        [Route("/notifications/{nid:guid}")]
        public async Task<IActionResult> UpdateNotification(Guid nid, bool toggleRead = false)
        {
            // Get the current user based on their claims principal. This is necessary to fetch their specific notifications.
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);

            // If for some reason we can't find the user (which shouldn't happen since this controller is protected by [Authorize]),
            // we return a 403 Forbidden response.
            if (user == null)
            {
                return Forbid();
            }

            // Find the specific notification by its GUID. This ensures the user can only update their own notifications.
            var notification = await _notificationRepo.FindByIdAsync(nid);

            // If we can't find the notification, return a 404 Not Found response.
            if (notification == null)
            {
                return NotFound();
            }

            // If toggleRead is true, we flip the IsRead status of the notification. This allows users to mark notifications as read/unread.
            if (toggleRead)
            {
                notification.IsRead = !notification.IsRead;
                await notification.SaveModelAsync(_notificationRepo);
            }

            // Return the updated notification as JSON. This can be used by the frontend to update the UI accordingly without a full page refresh.
            return Ok(notification);
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        [Route("/notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            await _notificationService.MarkAllAsReadAsync(user);

            return Ok(new { message = "All notifications marked as read." });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Route("/notifications/all")]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            await _notificationService.DeleteAllAsync(user);

            return Ok(new { message = "All notifications deleted." });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Route("/notifications/{nid:guid}")]
        public async Task<IActionResult> DeleteNotification(Guid nid)
        {
            // Get the current user based on their claims principal. This is necessary to fetch their specific notifications.
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);

            // Find the specific notification by its GUID. This ensures the user can only delete their own notifications.
            var notification = await _notificationRepo.FindByIdAsync(nid);

            // If we can't find the notification, return a 404 Not Found response.
            if (notification == null)
            {
                return NotFound();
            }

            // If for some reason we can't find the user (which shouldn't happen since this controller is protected by [Authorize]),
            // we return a 403 Forbidden response. Same for if the user tries to delete a notification that doesn't belong to them,
            // we want to return a 403 Forbidden to prevent unauthorized access.
            if (user == null || notification.RecipientId != user.GuidId)
            {
                return Forbid();
            }

            // Delete the notification from the database.
            await _notificationRepo.DeleteAsync(notification);

            // Return a success response. The frontend can use this to remove the notification from the UI without a full page refresh.
            return Ok(new { message = "Notification deleted successfully." });
        }

        [HttpGet("notification-preferences")]
        public async Task<IActionResult> NotificationPreferences()
        {
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);
            if (user == null) return Forbid();

            var viewModel = await BuildPreferencesViewModelAsync(user);
            return View(viewModel);
        }

        [HttpPost("notification-preferences")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotificationPreferences(NotificationPreferencesViewModel model)
        {
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);
            if (user == null) return Forbid();

            var updates = model.Preferences
                .Select(p => (p.NotificationType, p.SelectedChannel));

            await _notificationPreferenceService.SavePreferencesAsync(user, updates);

            _logger.LogInformation("User {Email} updated notification preferences", User.Identity?.Name);
            TempData["NotificationPreferencesSuccess"] = "Notification preferences saved.";
            return RedirectToAction(nameof(NotificationPreferences));
        }

        private async Task<NotificationPreferencesViewModel> BuildPreferencesViewModelAsync(ApplicationUser user)
        {
            var stored = (await _notificationPreferenceService.GetPreferencesAsync(user))
                .ToDictionary(p => p.NotificationType, p => p.DeliveryChannel);

            // Enumerate all NotificationType values that carry a [Display] attribute.
            // SystemCritical has no [Display] and is therefore excluded automatically —
            // any future type added to the enum just needs [Display] to appear here.
            var entries = Enum.GetValues<NotificationType>()
                .Select(t => (Type: t, Display: typeof(NotificationType)
                    .GetField(t.ToString())
                    ?.GetCustomAttribute<DisplayAttribute>()))
                .Where(x => x.Display != null)
                .Select(x => new NotificationPreferenceEntryViewModel
                {
                    NotificationType = x.Type,
                    DisplayName = x.Display!.Name ?? x.Type.ToString(),
                    Description = x.Display!.Description ?? string.Empty,
                    SelectedChannel = stored.GetValueOrDefault(x.Type, NotificationDeliveryChannel.InAppOnly)
                })
                .ToList();

            return new NotificationPreferencesViewModel { Preferences = entries };
        }

        // CSP-184: Dedicated Badges page *******************

        [HttpGet("badges")]
        public async Task<IActionResult> Badges()
        {
            // Get the user
            var user = await _userService.GetUserByClaimsPrincipleAsync(User);

            // Return 403 if user doesn't exist (Badges page is user-locked)
            if (user == null)
            {
                return Forbid();
            }

            // Get the user device's local timezone cookie for front-end Badge display
            //      Default timezone is PST
            string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";

            TimeZoneInfo userZone;
            try {
                userZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            } catch {
                // Fallback for environment-specific naming (e.g., Windows vs Linux)
                userZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }

            // Get all possible badges
            var allBadges = await _badgeRepo.GetAllAsync();

            // Get all badges the user has earned, convert the earned timestamps
            //      to the local timezone
            foreach (var ub in user.UserBadges)
            {
                if (ub.BadgeEarned.HasValue)
                {
                    // Convert UTC DB timestamp to display device's local time 
                    ub.BadgeEarned = TimeZoneInfo.ConvertTime(ub.BadgeEarned.Value, userZone);
                }
            }

            var viewModel = new BadgesViewModel
            {
                AllBadges = allBadges.OrderBy(b => b.Title).ToList(),
                UserBadges = user.UserBadges.Where(ub => ub.BadgeEarned.HasValue).ToList(),
                CurrentUserId = user.GuidId
            };

            /*
            // (Future work idea: add a toggle for sorting the Badges by time earned.
            //   Focus on connecting to View and getting Badges to display properly first.)
            */
            
            return View(viewModel);
        }
    }
}