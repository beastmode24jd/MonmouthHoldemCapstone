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

        public ClubsController(IClubService clubService,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
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

        public async Task<IActionResult> CreateNewClub()
        {
            // Need to create a new Club,
            //      then load and redirect to the Club's front page.

            
            /*
                public Club(Guid ownerId, string name, string? description, DateTimeOffset createdAt)
                {
                    OwnerId = ownerId;
                    Name = name;
                    Description = description;
                    CreatedAt = createdAt;
                }
            */

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

            // Need to pass this along to the ClubPage display, so it can show the created time
            //          accurately.

            return View("ClubPage");
        }
    }
}
