using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IClubService
    {
        Task<IEnumerable<Club>> GetPublicClubsAsync();

        // Returns private clubs the given user is a member of via ClubMembership.
        Task<IEnumerable<Club>> GetUserClubsAsync(Guid userId);

        // Creates a new club and auto-enrolls the owner as its first member.
        Task<Club> CreateClubAsync(Club club);
    }
}
