using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    // leaderboard is publicly accessible without login.
    public class LeaderboardController : Controller
    {
        private readonly ILeaderboardService _leaderboardService;

        public LeaderboardController(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            // Get the current page of users and the total count in parallel.
            var users = await _leaderboardService.GetLeaderboardPageAsync(page);
            var totalUsers = await _leaderboardService.GetTotalUserCountAsync();

            // Calculate total pages, rounding up so a partial page still counts.
            var totalPages = (int)Math.Ceiling(totalUsers / 30.0);

            // Read the logged in user's ID from their claims. If not logged in return null
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Only calculate rank if someone is logged in.
            var userRank = currentUserId != null
                ? await _leaderboardService.GetUserRankAsync(currentUserId)
                : 0;

            var vm = new LeaderboardViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentUserId = currentUserId,
                UserRank = userRank
            };

            return View(vm);
        }
    }
}