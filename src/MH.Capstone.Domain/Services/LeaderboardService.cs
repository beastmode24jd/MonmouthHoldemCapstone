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

        public Task<int> GetTotalUserCountAsync()
            => throw new NotImplementedException();

        public Task<int> GetUserRankAsync(string userId)
            => throw new NotImplementedException();
    }
}