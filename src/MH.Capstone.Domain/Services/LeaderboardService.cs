using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _userRepo;
        private readonly ILogger<LeaderboardService> _logger;

        public LeaderboardService(IRepository<ApplicationUser, ApplicationDbContext> userRepo, ILogger<LeaderboardService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<IEnumerable<ApplicationUser>> GetLeaderboardPageAsync(int page, int pageSize = 30)
        {
            // deactivated users should not appear on the leaderboard
            return (await _userRepo.GetAllAsync(u => !u.IsDeactivated))
                .OrderByDescending(u => u.Points) // sort by points descending
                .Skip((page - 1) * pageSize) // skip previous pages
                .Take(pageSize); // limit to page size
        }

        public async Task<int> GetTotalUserCountAsync()
        {
            // want to count only active users.
            // match the same filter as GetLeaderboardPageAsync to avoid counting deactivated users.
            return (await _userRepo.GetAllAsync(u => !u.IsDeactivated)).Count();
        }

        public async Task<int> GetUserRankAsync(string userId)
        {
            // Pull all active users ordered by points descending. same order as the leaderboard.
            var orderedUsers = (await _userRepo.GetAllAsync(u => !u.IsDeactivated))
                .OrderByDescending(u => u.Points)
                .Select(u => u.Id)
                .ToList();

            // FindIndex returns -1 if not found, so we add 1 to convert to 1-based rank.
            // If the user isn't in the list at all, we return 0 to signal "not found".
            var index = orderedUsers.FindIndex(id => id == userId);
            return index == -1 ? 0 : index + 1;
        }
    }
}