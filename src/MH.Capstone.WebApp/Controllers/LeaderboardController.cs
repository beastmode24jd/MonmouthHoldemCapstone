using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    // leaderboard is publicly accessible without login.
    public class LeaderboardController : Controller
    {
        private readonly ILeaderboardService _leaderboardService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaderboardController(ILeaderboardService leaderboardService, IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _leaderboardService = leaderboardService;
            _userService = userService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            // Get the current page of users and the total count in parallel.
            var users = (await _leaderboardService.GetLeaderboardPageAsync(page)).ToList();
            var totalUsers = await _userService.GetTotalUserCountAsync();

            // Calculate total pages, rounding up so a partial page still counts.
            var totalPages = (int)Math.Ceiling(totalUsers / 30.0);

            // Read the logged in user's ID using UserManager, following existing codebase patterns.
            var currentUserId = _userManager.GetUserId(User);

            // Only calculate rank if someone is logged in.
            var userRank = currentUserId != null
                ? await _leaderboardService.GetUserRankAsync(currentUserId)
                : 0;

            var entries = users.Select(u => new LeaderboardEntryViewModel
            {
                Id          = u.Id,
                DisplayName = u.DisplayName,
                Points      = u.Points
            }).ToList();

            var vm = new LeaderboardViewModel
            {
                Users = entries,
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentUserId = currentUserId,
                UserRank = userRank
            };

            return View(vm);
        }
    }
}