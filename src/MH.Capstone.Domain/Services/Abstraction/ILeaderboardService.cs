using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface ILeaderboardService
    {
        // Returns one page of users sorted by Points descending.
        Task<IEnumerable<ApplicationUser>> GetLeaderboardPageAsync(int page, int pageSize = 30);

        // Returns the 1-based rank of a given user by their ID.
        // Returns 0 if the user is not found.
        Task<int> GetUserRankAsync(string userId);
    }
}