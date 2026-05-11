using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    // CSP-180: Per-user opt-in page for real-time leaderboard / scoring notifications.
    [Authorize]
    [Route("dashboard/live-notifications")]
    public class LiveNotificationSettingsController : Controller
    {
        private readonly ILiveNotificationPreferenceService _preferences;
        private readonly UserManager<ApplicationUser> _userManager;

        public LiveNotificationSettingsController(
            ILiveNotificationPreferenceService preferences,
            UserManager<ApplicationUser> userManager)
        {
            _preferences = preferences;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var enabled = await _preferences.IsEnabledAsync(userId);
            return View(new LiveNotificationSettingsViewModel { Enabled = enabled });
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LiveNotificationSettingsViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            await _preferences.SetEnabledAsync(userId, model.Enabled);
            TempData["LiveNotificationsSaved"] = true;
            return RedirectToAction(nameof(Index));
        }
    }
}
