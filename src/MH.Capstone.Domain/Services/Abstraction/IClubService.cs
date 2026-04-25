using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IClubService
    {
        Task<IEnumerable<Club>> GetPublicClubsAsync();

        // Returns private clubs the given user is a member of via ClubMembership.
        Task<IEnumerable<Club>> GetUserClubsAsync(Guid userId);

        // Returns a single club with its Owner nav property loaded, or null if not found.
        Task<Club?> GetClubByIdAsync(Guid id);

        // Creates a new club and auto-enrolls the owner as its first member.
        Task<Club> CreateClubAsync(Club club);

        // Sends an invite from a member of a club to a non-member user.
        Task SendInviteAsync(Club club, Guid senderId, Guid receiverId);

        // Accepts an invite from a member of a club as a former non-member user.
        Task AcceptInviteAsync(Club club, Guid senderId, Guid receiverId);

        Task DeclineInviteAsync(Club club, Guid senderId, Guid receiverId);
    }
}
