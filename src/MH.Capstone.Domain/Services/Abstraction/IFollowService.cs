namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IFollowService
    {
        // Creates a UserFollow row from follower -> followee and dispatches a NewFollower notification.
        // Idempotent: a second call for the same pair is a no-op (no duplicate row, no second notification).
        // Throws InvalidOperationException if followerId == followeeId.
        Task FollowAsync(Guid followerId, Guid followeeId);

        // Removes the UserFollow row from follower -> followee.
        // Idempotent: silently no-ops if no row exists.
        Task UnfollowAsync(Guid followerId, Guid followeeId);

        // Returns true if follower currently follows followee.
        Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId);

        // Returns the set of user IDs that the given user follows.
        Task<IEnumerable<Guid>> GetFolloweeIdsAsync(Guid followerId);

        // Returns the set of user IDs that follow the given user (inbound direction).
        Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid followeeId);
    }
}
