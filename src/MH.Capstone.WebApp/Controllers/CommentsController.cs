using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    // CSP-187 AC2 + AC4: post comments + moderate (admin-only).
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            ICommentService commentService,
            UserManager<ApplicationUser> userManager,
            ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _userManager = userManager;
            _logger = logger;
        }

        // POST /Sighting/{sightingId}/Comments
        [HttpPost("/Sighting/{sightingId:guid}/Comments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid sightingId, string body)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            try
            {
                await _commentService.PostCommentAsync(sightingId, user.GuidId, body ?? string.Empty);
            }
            catch (ArgumentException ex)
            {
                TempData["CommentError"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["CommentError"] = ex.Message;
            }

            return RedirectToAction("Details", "Sighting", new { id = sightingId });
        }

        // POST /Comments/{commentId}/Hide — admin only
        [HttpPost("/Comments/{commentId:guid}/Hide")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(Guid commentId, string? reason, Guid sightingId)
        {
            var moderator = await _userManager.GetUserAsync(User);
            if (moderator == null) return Challenge();

            await _commentService.HideAsync(commentId, moderator.GuidId, reason);
            return RedirectToAction("Details", "Sighting", new { id = sightingId });
        }

        // POST /Comments/{commentId}/Reinstate — admin only
        [HttpPost("/Comments/{commentId:guid}/Reinstate")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reinstate(Guid commentId, Guid sightingId)
        {
            var moderator = await _userManager.GetUserAsync(User);
            if (moderator == null) return Challenge();

            await _commentService.ReinstateAsync(commentId, moderator.GuidId);
            return RedirectToAction("Details", "Sighting", new { id = sightingId });
        }
    }
}
