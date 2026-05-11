using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface ICommentService
    {
        // Persists a new comment on a sighting and dispatches a NewComment notification
        // to the sighting owner (unless the author is the owner). Enforces a 5-per-minute
        // rate limit per author; throws InvalidOperationException when exceeded.
        // Throws ArgumentException if body is empty/whitespace.
        Task<Comment> PostCommentAsync(Guid sightingId, Guid authorId, string body);

        // Returns visible comments for a sighting, oldest-first.
        // Excludes hidden comments and (when viewerId is supplied) comments authored by
        // users the viewer has blocked.
        Task<IEnumerable<Comment>> GetCommentsForSightingAsync(Guid sightingId, Guid? viewerId = null);

        // Marks a comment hidden and writes a CommentModerationLog audit row.
        // No-op if the comment is already hidden.
        Task HideAsync(Guid commentId, Guid moderatorId, string? reason);

        // Clears the hidden flag and writes a Reinstated audit row.
        // No-op if the comment is not currently hidden.
        Task ReinstateAsync(Guid commentId, Guid moderatorId);
    }
}
