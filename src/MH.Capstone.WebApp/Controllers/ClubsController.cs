using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MH.Capstone.WebApp.Controllers
{
    [Authorize]
    public class ClubsController : Controller
    {
        private readonly IClubService _clubService;
        private readonly UserManager<ApplicationUser> _userManager;

        // To notify user if they have been added to/invited to a Club.
        private readonly INotificationService _notificationService;

        private readonly ILogger<ClubsController> _logger;

        public ClubsController(ILogger<ClubsController> logger,
            IClubService clubService,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _logger = logger;
            _clubService = clubService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            var publicClubs = await _clubService.GetPublicClubsAsync();
            var userClubs = await _clubService.GetUserClubsAsync(user.GuidId);

            var viewModel = new ClubListViewModel(publicClubs, userClubs, user.Id);

            return View("LandingPage", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClub(string name, string? description, bool isPublic)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            // Initialize the club.

            Guid ownerId = user.GuidId;
            DateTimeOffset createdAt = DateTimeOffset.UtcNow;

            var newClub = new Club
            {
                OwnerId = ownerId,
                Name = name,
                Description = description,
                IsPublic = isPublic,
                CreatedAt = createdAt
            };

            // Save the club
            await _clubService.CreateClubAsync(newClub);

            if (!newClub.IsPublic)
            {
                _logger.LogInformation("Saved user {Email}'s private Club {Name}", 
                user.Email, newClub.Name);
            }

            // TODO: redirect to ClubPage

            // Get timezone cookie from site.js (defaults to PST if not found)
            string userTimeZoneId = Request.Cookies["UserTimeZone"] ?? "America/Los_Angeles";
            
            TimeZoneInfo userZone;

            try
            {
                userZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            }
            catch
            {
                // Windows server fallback for if IANA string fails
                userZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }

            // Convert the timestamp to the device's actual zone
            DateTimeOffset deviceTime = TimeZoneInfo.ConvertTime((DateTimeOffset)newClub.CreatedAt, userZone);

            // Generate the notification with the correct AM/PM and 12-hour format
            string timeDisplay = deviceTime.ToString("MM/dd/yyyy h:mm tt");

            await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId,
                $"Made the {newClub.Name} Club",
                $"Good work. Keep at it!"
                ));

            // Need to pass this along to the ClubPage display,
            //      so it can show the created time accurately.

            string clubId = newClub.Id.ToString();

            ViewData["ClubCreatedAt"] = timeDisplay;
            ViewData["ClubIDValue"] = clubId;

            return View("ClubPage");
        }
    }
}
