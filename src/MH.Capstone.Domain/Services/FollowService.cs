using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class FollowService : IFollowService
    {
        private readonly IRepository<UserFollow, ApplicationDbContext> _followRepo;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public FollowService(
            IRepository<UserFollow, ApplicationDbContext> followRepo,
            INotificationService notificationService,
            IUserService userService)
        {
            _followRepo = followRepo;
            _notificationService = notificationService;
            _userService = userService;
        }

        public async Task FollowAsync(Guid followerId, Guid followeeId)
        {
            if (followerId == followeeId)
                throw new InvalidOperationException("A user cannot follow themselves.");

            var followerKey = followerId.ToString();
            var followeeKey = followeeId.ToString();

            var existing = (await _followRepo.GetAllAsync(
                f => f.FollowerIdentityId == followerKey && f.FolloweeIdentityId == followeeKey))
                .FirstOrDefault();

            if (existing != null) return;

            await _followRepo.AddOrUpdateAsync(new UserFollow(followerId, followeeId));

            var follower = await _userService.GetUserByIdAsync(followerId);
            var followerName = follower?.DisplayName ?? "Someone";

            await _notificationService.SendNotificationAsync(
                Notification.Create(
                    followeeId,
                    "New Follower",
                    $"{followerName} started following you."),
                NotificationType.NewFollower);
        }

        public async Task UnfollowAsync(Guid followerId, Guid followeeId)
        {
            var followerKey = followerId.ToString();
            var followeeKey = followeeId.ToString();

            var existing = (await _followRepo.GetAllAsync(
                f => f.FollowerIdentityId == followerKey && f.FolloweeIdentityId == followeeKey))
                .FirstOrDefault();

            if (existing == null) return;

            await _followRepo.DeleteAsync(existing);
        }

        public async Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId)
        {
            var followerKey = followerId.ToString();
            var followeeKey = followeeId.ToString();

            return (await _followRepo.GetAllAsync(
                f => f.FollowerIdentityId == followerKey && f.FolloweeIdentityId == followeeKey))
                .Any();
        }

        public async Task<IEnumerable<Guid>> GetFolloweeIdsAsync(Guid followerId)
        {
            var followerKey = followerId.ToString();
            var rows = await _followRepo.GetAllAsync(f => f.FollowerIdentityId == followerKey);
            return rows.AsEnumerable().Select(f => f.FolloweeId).ToList();
        }

        public async Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid followeeId)
        {
            var followeeKey = followeeId.ToString();
            var rows = await _followRepo.GetAllAsync(f => f.FolloweeIdentityId == followeeKey);
            return rows.AsEnumerable().Select(f => f.FollowerId).ToList();
        }
    }
}
