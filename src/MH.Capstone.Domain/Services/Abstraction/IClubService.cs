using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IClubService
    {
        Task<IEnumerable<Club>> GetPublicClubsAsync();

        // Returns clubs the given user is an accepted member of (AcceptedInvite == true).
        Task<IEnumerable<Club>> GetUserClubsAsync(Guid userId);

        // Returns clubs the given user has a pending (unaccepted) invite to.
        Task<IEnumerable<Club>> GetPendingInvitesAsync(Guid userId);

        // Returns all membership rows (accepted + pending) for a given club.
        Task<IEnumerable<ClubMembership>> GetClubMembershipsAsync(Guid clubId);

        // Returns a single club with its Owner nav property loaded, or null if not found.
        Task<Club?> GetClubByIdAsync(Guid id);

        // Creates a new club and auto-enrolls the owner as its first accepted member.
        Task<Club> CreateClubAsync(Club club);

        // Creates a pending membership row (AcceptedInvite = false) for receiverId.
        // Throws InvalidOperationException if sender is not a member or receiver is already invited/a member.
        Task SendInviteAsync(Club club, Guid senderId, Guid receiverId);

        // Flips the pending membership row to AcceptedInvite = true.
        // Throws InvalidOperationException if no pending invite exists.
        Task AcceptInviteAsync(Guid clubId, Guid userId);

        // Deletes the pending membership row.
        // Throws InvalidOperationException if no pending invite exists.
        Task DeclineInviteAsync(Guid clubId, Guid userId);

        // Deletes the accepted membership row for userId.
        // Throws InvalidOperationException if userId is the club owner or is not a member.
        Task LeaveClubAsync(Guid clubId, Guid userId);

        // Saves a new message to the club's chatroom log.
        // Throws ArgumentException if content is empty.
        Task SendMessageAsync(Guid clubId, Guid senderId, string content);

        // Returns all messages for a club, ordered oldest-first, with Author eagerly loaded.
        Task<List<Message>> GetClubMessagesAsync(Guid clubId);
    }
}
