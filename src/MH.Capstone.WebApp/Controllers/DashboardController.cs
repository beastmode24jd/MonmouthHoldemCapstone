using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Constants;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
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
        //private readonly Guid PROFILE_BADGE_GUID = MH.Capstone.Domain.Constants.BadgeId.ProfileBadgeGUID;
        //private readonly Guid BIO_BADGE_GUID = MH.Capstone.Domain.Constants.BadgeId.CustomBioBadgeGUID;
        //private readonly Guid SIGHTING_BADGE_GUID = MH.Capstone.Domain.Constants.BadgeId.FirstSightingBadgeGUID;
        
        // Logger to track dashboard access and activity. 
        private readonly ILogger<DashboardController> _logger;

        // Dep. Injection for image upload, profile bio, and authentication services.
        private readonly IProfileImageService _imageService;

        private readonly IAuthenticationService _authService;

        private readonly IUserService _userService;

        private readonly INotificationService _notificationService;

        private readonly IRepository<Notification, ApplicationDbContext> _notificationRepo;

        private readonly IBadgeService _badgeService;

        // Constructor that injects the logger dependency
        public DashboardController(ILogger<DashboardController> logger, 
            IProfileImageService imageService, IAuthenticationService authService, 
            IBadgeService badgeService, INotificationService notificationService, 
            IUserService userService, IRepository<Notification, ApplicationDbContext> notificationRepo)
        {
            _logger = logger;
            _imageService = imageService;
            _authService = authService;
            _badgeService = badgeService;
            _notificationService = notificationService;
            _userService = userService;
            _notificationRepo = notificationRepo;
        }

        // Displays the main dashboard page for authenticated users. 
        public async Task<IActionResult> Index([FromQuery] bool sighting_success = false,
            [FromQuery] int? points_earned = null)
        {
            var userEmail = User.Identity?.Name;
            var user = await _userService.GetUserByEmailAsync(userEmail ?? "");
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

            // Need to check if user has badges on initial page load
            if (user is { UserBadges.Count: 0 })
            {
                if (user.Bio != null)
                {
                    await _badgeService.AddBadge(user, BadgeId.CustomBioBadgeGUID);
                }

                if (user.Sightings.Count >= 1)
                {
                    await _badgeService.AddBadge(user, BadgeId.FirstSightingBadgeGUID);
                }

                if (user.ProfileImage != null)
                {
                    await _badgeService.AddBadge(user, BadgeId.ProfileBadgeGUID);
                }
            }

            // Get sorted Badges for display
            var sortedBadges = new List<UserBadge>();
            if (user != null)
            {
                sortedBadges = await _badgeService.SortBadgesByTime(user.UserBadges);
            }

            ViewData["SortedBadges"] = sortedBadges;
            ViewData["statusMsgHtml"] = statusMsgHtml;
            return View();
        }

        [HttpPost]
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
                    return RedirectToAction("Index");
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
                    return RedirectToAction("Index");
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

                    // Add the Custom Profile Badge to the User object
                    await _badgeService.AddBadge(user, BadgeId.ProfileBadgeGUID);
                }
            }
            else
            {
                _logger.LogWarning("Upload attempted with null or empty file.");
            }

            // Send this information back to the main dashboard page.
            return RedirectToAction("Index");
        }

        [HttpPost]
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

                // Add the custom Bio Badge to the UserBadges list.
                // Does not add the badge if the user already has a custom bio.
                await _badgeService.AddBadge(user, BadgeId.CustomBioBadgeGUID);
            }

            return RedirectToAction("Index");
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
    }
}