using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ApplicationDbContext _context;

        public LeaderboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ApplicationUser>> GetLeaderboardPageAsync(int page, int pageSize = 30)
        {
            return await _context.Users
                .Where(u => !u.IsDeactivated) // deactivated users should not appear on the leaderboard
                .OrderByDescending(u => u.Points) // sort by points descending
                .Skip((page - 1) * pageSize) // skip previous pages
                .Take(pageSize) // limit to page size
                .ToListAsync();
        }

        public async Task<int> GetTotalUserCountAsync()
        {
            // want to count only active users.
            // match the same filter as GetLeaderboardPageAsync to avoid counting deactivated users.
            return await _context.Users
                .Where(u => !u.IsDeactivated)
                .CountAsync();
        }

        public async Task<int> GetUserRankAsync(string userId)
        {
            // Pull all active users ordered by points descending. same order as the leaderboard.
            var orderedUsers = await _context.Users
                .Where(u => !u.IsDeactivated)
                .OrderByDescending(u => u.Points)
                .Select(u => u.Id)
                .ToListAsync();

            // FindIndex returns -1 if not found, so we add 1 to convert to 1-based rank.
            // If the user isn't in the list at all, we return 0 to signal "not found".
            var index = orderedUsers.FindIndex(id => id == userId);
            return index == -1 ? 0 : index + 1;
        }
    }
}