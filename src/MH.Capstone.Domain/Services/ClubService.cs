using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class ClubService : IClubService
    {
        /* IMPORTANT FUTURE CLUB SERVICE FILE NOTE!!!

            "Deleting a user will now throw if they still have club memberships or messages.
            Your service layer will need to clean those up before deleting a user."

        */
        private readonly IRepository<Club, ApplicationDbContext> _clubRepo;
        private readonly IRepository<ClubMembership, ApplicationDbContext> _membershipRepo;

        public ClubService(
            IRepository<Club, ApplicationDbContext> clubRepo,
            IRepository<ClubMembership, ApplicationDbContext> membershipRepo)
        {
            _clubRepo = clubRepo;
            _membershipRepo = membershipRepo;
        }

        public async Task<IEnumerable<Club>> GetPublicClubsAsync()
        {
            return (await _clubRepo.GetAllAsync()).Where(c => c.IsPublic);
        }

        public async Task<IEnumerable<Club>> GetUserClubsAsync(Guid userId)
        {
            // Fetch the clubs this user is a member of.
            // Mirrors GetUserSightingsAsync:
            //      Uses the predicate overload to push filtering to the DB layer.
            var membershipQuery = await _membershipRepo.GetAllAsync(m => m.MemberIdentityId == userId.ToString());
            var memberClubIds = membershipQuery.Select(m => m.ClubId).ToHashSet();

            var clubQuery = await _clubRepo.GetAllAsync(c => memberClubIds.Contains(c.Id));
            return clubQuery.OrderBy(c => c.Name).ToList();
        }

        public async Task<Club?> GetClubByIdAsync(Guid id)
        {
            // Includes Owner so the controller/view can display the owner's username
            // without relying on lazy loading.
            return (await _clubRepo.GetAllAsync(c => c.Owner))
                .FirstOrDefault(c => c.Id == id);
        }

        public async Task<Club> CreateClubAsync(Club club)
        {
            if (club == null)
                throw new ArgumentNullException(nameof(club));

            var savedClub = await _clubRepo.AddOrUpdateAsync(club);

            // Auto-enroll the owner as the first member so they appear in their own "My Clubs" list.
            var ownerMembership = new ClubMembership(savedClub.OwnerId, savedClub.Id, savedClub.CreatedAt);
            await _membershipRepo.AddOrUpdateAsync(ownerMembership);

            return savedClub;
        }

        // Sends an invite from a member of a club to a non-member user.
        public async Task SendInviteAsync(Club club, Guid senderId, Guid receiverId)
        {
            
        }

        // Accepts an invite from a member of a club as a former non-member user.
        public async Task AcceptInviteAsync(Club club, Guid senderId, Guid receiverId)
        {
            
        }

        // Declines an invite from a member of a club as the non-member user.
        public async Task DeclineInviteAsync(Club club, Guid senderId, Guid receiverId)
        {
            
        }

    }
}
