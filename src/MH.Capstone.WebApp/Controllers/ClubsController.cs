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
        private readonly INotificationService _notificationService;
        private readonly ILogger<ClubsController> _logger;

        public ClubsController(
            ILogger<ClubsController> logger,
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
            var pendingInvites = await _clubService.GetPendingInvitesAsync(user.GuidId);

            var viewModel = new ClubListViewModel(publicClubs, userClubs, user.Id, pendingInvites);
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

            var members = (await _clubService.GetClubMembershipsAsync(id))
                .Where(m => m.AcceptedInvite)
                .ToList();

            var sightings = await _clubService.GetClubSightingsAsync(id);

            bool hasSightings = false;
            if (sightings.Count > 0)
            {
                hasSightings = true;
            }

            return View("ClubPage", new ClubPageViewModel(club, members, sightings, hasSightings, isOwner, isMember));
        }

        [HttpGet]
        public async Task<IActionResult> Chatroom(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            var club = await _clubService.GetClubByIdAsync(id);
            if (club == null)
                return NotFound();

            var userClubs = await _clubService.GetUserClubsAsync(user.GuidId);
            bool isMember = userClubs.Any(c => c.Id == id);

            if (!club.IsPublic && !isMember)
                return Forbid();

            var messages = await _clubService.GetClubMessagesAsync(id);
            return View("Chatroom", new ClubMessageViewModel(club, messages, user.Id, isMember));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(Guid clubId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["MessageError"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Chatroom), new { id = clubId });
            }

            try
            {
                await _clubService.SendMessageAsync(clubId, user.GuidId, content);
            }
            catch (ArgumentException ex)
            {
                TempData["MessageError"] = ex.Message;
            }

            return RedirectToAction(nameof(Chatroom), new { id = clubId });
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

            await _notificationService.SendNotificationAsync(
                Notification.Create(user.GuidId,
                    ClubNotificationTitle($"Made the {newClub.Name} Club"),
                    "Good work. Keep at it!"),
                NotificationType.ClubActivity);

            return RedirectToAction(nameof(ClubPage), new { id = newClub.Id });
        }

        // Returns JSON [{id, displayName}] of users matching the query, excluding current club members.
        [HttpGet]
        public async Task<IActionResult> SearchUsers(Guid clubId, string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(Array.Empty<object>());

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            var existingMemberships = await _clubService.GetClubMembershipsAsync(clubId);
            var excludedIds = existingMemberships.Select(m => m.MemberIdentityId).ToHashSet();
            excludedIds.Add(currentUser.Id);

            var results = _userManager.Users
                .Where(u => !excludedIds.Contains(u.Id)
                         && u.DisplayName != "UNSET"
                         && u.DisplayName.Contains(query))
                .Take(10)
                .Select(u => new { id = u.Id, displayName = u.DisplayName })
                .ToList();

            return Json(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvite(Guid clubId, string receiverId)
        {
            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            var club = await _clubService.GetClubByIdAsync(clubId);
            if (club == null)
                return NotFound();

            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null)
            {
                TempData["InviteError"] = "User not found.";
                return RedirectToAction(nameof(ClubPage), new { id = clubId });
            }

            try
            {
                await _clubService.SendInviteAsync(club, sender.GuidId, receiver.GuidId);

                await _notificationService.SendNotificationAsync(
                    Notification.Create(receiver.GuidId,
                        ClubNotificationTitle($"You've been invited to join {club.Name}"),
                        $"{sender.DisplayName} has invited you to their club. Visit Clubs to accept or decline."),
                    NotificationType.ClubActivity);

                TempData["InviteSuccess"] = $"Invite sent to {receiver.DisplayName}.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["InviteError"] = ex.Message;
            }

            return RedirectToAction(nameof(ClubPage), new { id = clubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptInvite(Guid clubId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            try
            {
                await _clubService.AcceptInviteAsync(clubId, user.GuidId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("AcceptInvite failed for user {UserId} on club {ClubId}: {Message}",
                    user.Id, clubId, ex.Message);
                TempData["InviteError"] = "Could not accept invite.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(ClubPage), new { id = clubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineInvite(Guid clubId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            try
            {
                await _clubService.DeclineInviteAsync(clubId, user.GuidId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("DeclineInvite failed for user {UserId} on club {ClubId}: {Message}",
                    user.Id, clubId, ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveClub(Guid clubId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return StatusCode((int)HttpStatusCode.InternalServerError);

            try
            {
                await _clubService.LeaveClubAsync(clubId, user.GuidId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("LeaveClub failed for user {UserId} on club {ClubId}: {Message}",
                    user.Id, clubId, ex.Message);
                TempData["LeaveError"] = ex.Message;
                return RedirectToAction(nameof(ClubPage), new { id = clubId });
            }

            return RedirectToAction(nameof(Index));
        }

        // Notification.Title is nvarchar(50) — truncate club-related titles to fit.
        private static string ClubNotificationTitle(string title) =>
            title.Length <= 50 ? title : string.Concat(title.AsSpan(0, 47), "...");
    }
}
