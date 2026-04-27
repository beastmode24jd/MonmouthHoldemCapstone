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

        [HttpGet]
        public async Task<IActionResult> ClubPage(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            var club = await _clubService.GetClubByIdAsync(id);
            if (club == null)
                return NotFound();

            var userClubs = await _clubService.GetUserClubsAsync(user.GuidId);
            bool isMember = userClubs.Any(c => c.Id == id);
            bool isOwner = club.OwnerId == user.GuidId;

            if (!club.IsPublic && !isMember)
                return Forbid();

            return View("ClubPage", new ClubPageViewModel(club, isOwner, isMember));
        }

        [HttpGet]
        public IActionResult Chatroom(Guid id)
        {
            return View("Chatroom");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClub(string name, string? description, bool isPublic)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            var newClub = new Club
            {
                OwnerId = user.GuidId,
                Name = name,
                Description = description,
                IsPublic = isPublic,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _clubService.CreateClubAsync(newClub);

            if (!newClub.IsPublic)
                _logger.LogInformation("Saved user {Email}'s private Club {Name}.", user.Email, newClub.Name);

            await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId,
                $"Made the {newClub.Name} Club",
                "Good work. Keep at it!"),
                NotificationType.ClubActivity);

            return RedirectToAction(nameof(ClubPage), new { id = newClub.Id });
        }
    }
}
