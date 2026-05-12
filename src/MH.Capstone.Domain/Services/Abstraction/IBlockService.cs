namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IBlockService
    {
        // Creates a UserBlock row from blocker -> blocked. No notification sent (blocking is silent).
        // Idempotent: a second call for the same pair is a no-op.
        // Throws InvalidOperationException if blockerId == blockedId.
        Task BlockAsync(Guid blockerId, Guid blockedId);

        // Removes the UserBlock row from blocker -> blocked.
        // Idempotent: silently no-ops if no row exists.
        Task UnblockAsync(Guid blockerId, Guid blockedId);

        // Returns true if blocker currently blocks blocked.
        Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId);

        // Returns the set of user IDs that the given user has blocked. Used to filter feeds and comments.
        Task<IEnumerable<Guid>> GetBlockedUserIdsAsync(Guid blockerId);
    }
}
