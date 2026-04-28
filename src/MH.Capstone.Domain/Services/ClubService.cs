using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class ClubService : IClubService
    {
        /* 
            Important note for if we build out future permanent account deletion:
            
            "Deleting a user will now throw if they still have club memberships or messages.
            Your service layer will need to clean those up before deleting a user."
        */
        private readonly IRepository<Club, ApplicationDbContext> _clubRepo;
        private readonly IRepository<ClubMembership, ApplicationDbContext> _membershipRepo;
        private readonly IRepository<Message, ApplicationDbContext> _messageRepo;

        public ClubService(
            IRepository<Club, ApplicationDbContext> clubRepo,
            IRepository<ClubMembership, ApplicationDbContext> membershipRepo,
            IRepository<Message, ApplicationDbContext> messageRepo)
        {
            _clubRepo = clubRepo;
            _membershipRepo = membershipRepo;
            _messageRepo = messageRepo;
        }

        public async Task<IEnumerable<Club>> GetPublicClubsAsync()
        {
            return (await _clubRepo.GetAllAsync()).Where(c => c.IsPublic);
        }

        public async Task<IEnumerable<Club>> GetUserClubsAsync(Guid userId)
        {
            var memberships = await _membershipRepo.GetAllAsync(
                m => m.MemberIdentityId == userId.ToString() && m.AcceptedInvite);
            var memberClubIds = memberships.Select(m => m.ClubId).ToHashSet();

            var clubs = await _clubRepo.GetAllAsync(c => memberClubIds.Contains(c.Id));
            return clubs.OrderBy(c => c.Name).ToList();
        }

        public async Task<IEnumerable<Club>> GetPendingInvitesAsync(Guid userId)
        {
            var pending = await _membershipRepo.GetAllAsync(
                m => m.MemberIdentityId == userId.ToString() && !m.AcceptedInvite);
            var pendingClubIds = pending.Select(m => m.ClubId).ToHashSet();

            if (!pendingClubIds.Any())
                return Enumerable.Empty<Club>();

            return await _clubRepo.GetAllAsync(c => pendingClubIds.Contains(c.Id));
        }

        public async Task<IEnumerable<ClubMembership>> GetClubMembershipsAsync(Guid clubId)
        {
            return await _membershipRepo.GetAllAsync(m => m.ClubId == clubId);
        }

        public async Task<Club?> GetClubByIdAsync(Guid id)
        {
            return (await _clubRepo.GetAllAsync(c => c.Owner))
                .FirstOrDefault(c => c.Id == id);
        }

        public async Task<Club> CreateClubAsync(Club club)
        {
            if (club == null)
                throw new ArgumentNullException(nameof(club));

            var savedClub = await _clubRepo.AddOrUpdateAsync(club);

            // Owner is always an accepted member of their own club.
            var ownerMembership = new ClubMembership(savedClub.OwnerId, savedClub.Id, savedClub.CreatedAt)
            {
                AcceptedInvite = true
            };
            await _membershipRepo.AddOrUpdateAsync(ownerMembership);

            return savedClub;
        }

        public async Task SendInviteAsync(Club club, Guid senderId, Guid receiverId)
        {
            var clubMemberships = (await _membershipRepo.GetAllAsync(
                m => m.ClubId == club.Id)).ToList();

            bool senderIsMember = clubMemberships.Any(m => m.MemberId == senderId && m.AcceptedInvite);
            if (!senderIsMember)
                throw new InvalidOperationException("Sender is not a member of this club.");

            bool receiverAlreadyPresent = clubMemberships.Any(m => m.MemberId == receiverId);
            if (receiverAlreadyPresent)
                throw new InvalidOperationException("User is already a member or has a pending invite.");

            var invite = new ClubMembership(receiverId, club.Id, DateTimeOffset.UtcNow)
            {
                AcceptedInvite = false
            };
            await _membershipRepo.AddOrUpdateAsync(invite);
        }

        public async Task AcceptInviteAsync(Guid clubId, Guid userId)
        {
            var pending = (await _membershipRepo.GetAllAsync(
                m => m.ClubId == clubId && m.MemberIdentityId == userId.ToString() && !m.AcceptedInvite))
                .FirstOrDefault();

            if (pending == null)
                throw new InvalidOperationException("No pending invite found.");

            pending.AcceptedInvite = true;
            await _membershipRepo.AddOrUpdateAsync(pending);
        }

        public async Task DeclineInviteAsync(Guid clubId, Guid userId)
        {
            var pending = (await _membershipRepo.GetAllAsync(
                m => m.ClubId == clubId && m.MemberIdentityId == userId.ToString() && !m.AcceptedInvite))
                .FirstOrDefault();

            if (pending == null)
                throw new InvalidOperationException("No pending invite found.");

            await _membershipRepo.DeleteAsync(pending);
        }

        public async Task LeaveClubAsync(Guid clubId, Guid userId)
        {
            var club = await _clubRepo.FindByIdAsync(clubId);
            if (club == null)
                throw new InvalidOperationException("Club not found.");

            if (club.OwnerId == userId)
                throw new InvalidOperationException("The club owner cannot leave their own club.");

            var membership = (await _membershipRepo.GetAllAsync(
                m => m.ClubId == clubId && m.MemberIdentityId == userId.ToString() && m.AcceptedInvite))
                .FirstOrDefault();

            if (membership == null)
                throw new InvalidOperationException("User is not a member of this club.");

            await _membershipRepo.DeleteAsync(membership);
        }

        public async Task SendMessageAsync(Guid clubId, Guid senderId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.", nameof(content));

            if (content.Trim().Length > 2000)
                throw new ArgumentException("Message cannot exceed 2000 characters.", nameof(content));

            var message = new Message(clubId, senderId, content.Trim(), DateTimeOffset.UtcNow);
            await _messageRepo.AddOrUpdateAsync(message);
        }

        public async Task<List<Message>> GetClubMessagesAsync(Guid clubId)
        {
            return (await _messageRepo.GetAllAsync(m => m.Author))
                .Where(m => m.ClubId == clubId)
                .OrderBy(m => m.SentAt)
                .ToList();
        }

    }
}
