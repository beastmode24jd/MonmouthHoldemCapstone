using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IClubService
    {
        Task<IEnumerable<Club>> GetPublicClubsAsync();

        // Returns private clubs the given user is a member of via ClubMembership.
        Task<IEnumerable<Club>> GetUserClubsAsync(Guid userId);
    }
}
