using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ApplicationDbContext _context;

        public LeaderboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<ApplicationUser>> GetLeaderboardPageAsync(int page, int pageSize = 30)
            => throw new NotImplementedException();

        public Task<int> GetTotalUserCountAsync()
            => throw new NotImplementedException();

        public Task<int> GetUserRankAsync(string userId)
            => throw new NotImplementedException();
    }
}