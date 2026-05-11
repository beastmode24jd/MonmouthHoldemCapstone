using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class CommentService : ICommentService
    {
        private const int RateLimitPerMinute = 5;

        private readonly IRepository<Comment, ApplicationDbContext> _commentRepo;
        private readonly IRepository<CommentModerationLog, ApplicationDbContext> _logRepo;
        private readonly IRepository<Sighting, ApplicationDbContext> _sightingRepo;
        private readonly IBlockService _blockService;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public CommentService(
            IRepository<Comment, ApplicationDbContext> commentRepo,
            IRepository<CommentModerationLog, ApplicationDbContext> logRepo,
            IRepository<Sighting, ApplicationDbContext> sightingRepo,
            IBlockService blockService,
            INotificationService notificationService,
            IUserService userService)
        {
            _commentRepo = commentRepo;
            _logRepo = logRepo;
            _sightingRepo = sightingRepo;
            _blockService = blockService;
            _notificationService = notificationService;
            _userService = userService;
        }

        public async Task<Comment> PostCommentAsync(Guid sightingId, Guid authorId, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Comment body cannot be empty.", nameof(body));

            var sighting = await _sightingRepo.FindByIdAsync(sightingId)
                ?? throw new InvalidOperationException("Sighting not found.");

            var authorKey = authorId.ToString();
            var oneMinuteAgo = DateTimeOffset.UtcNow.AddMinutes(-1);
            var recentByAuthor = await _commentRepo.GetAllAsync(
                c => c.AuthorIdentityId == authorKey && c.CreatedAt > oneMinuteAgo);

            if (recentByAuthor.Count() >= RateLimitPerMinute)
                throw new InvalidOperationException("Rate limit exceeded; try again in a minute.");

            var trimmed = body.Trim();
            var comment = new Comment(sightingId, authorId, trimmed);
            var saved = await _commentRepo.AddOrUpdateAsync(comment);

            // Skip self-notification if commenter is the sighting owner.
            var ownerId = Guid.Parse(sighting.UserIdentityId);
            if (ownerId != authorId)
            {
                var author = await _userService.GetUserByIdAsync(authorId);
                var authorName = author?.DisplayName ?? "Someone";
                var preview = trimmed.Length > 80 ? trimmed.Substring(0, 80) + "…" : trimmed;

                await _notificationService.SendNotificationAsync(
                    Notification.Create(
                        ownerId,
                        "New Comment",
                        $"{authorName} commented: \"{preview}\""),
                    NotificationType.NewComment);
            }

            return saved;
        }

        public async Task<IEnumerable<Comment>> GetCommentsForSightingAsync(Guid sightingId, Guid? viewerId = null)
        {
            var visible = (await _commentRepo.GetAllAsync(
                c => c.SightingId == sightingId && !c.IsHidden))
                .OrderBy(c => c.CreatedAt)
                .ToList();

            if (viewerId == null)
                return visible;

            var blocked = (await _blockService.GetBlockedUserIdsAsync(viewerId.Value)).ToHashSet();
            if (blocked.Count == 0)
                return visible;

            return visible.Where(c => !blocked.Contains(Guid.Parse(c.AuthorIdentityId))).ToList();
        }

        public async Task HideAsync(Guid commentId, Guid moderatorId, string? reason)
        {
            var comment = await _commentRepo.FindByIdAsync(commentId)
                ?? throw new InvalidOperationException("Comment not found.");

            if (comment.IsHidden) return;

            comment.IsHidden = true;
            comment.HiddenAt = DateTimeOffset.UtcNow;
            comment.HiddenByIdentityId = moderatorId.ToString();
            comment.HiddenReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            await _commentRepo.AddOrUpdateAsync(comment);

            await _logRepo.AddOrUpdateAsync(new CommentModerationLog
            {
                CommentId = commentId,
                ModeratorId = moderatorId,
                Action = CommentModerationAction.Hidden,
                Reason = comment.HiddenReason,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        public async Task ReinstateAsync(Guid commentId, Guid moderatorId)
        {
            var comment = await _commentRepo.FindByIdAsync(commentId)
                ?? throw new InvalidOperationException("Comment not found.");

            if (!comment.IsHidden) return;

            comment.IsHidden = false;
            comment.HiddenAt = null;
            comment.HiddenByIdentityId = null;
            comment.HiddenReason = null;

            await _commentRepo.AddOrUpdateAsync(comment);

            await _logRepo.AddOrUpdateAsync(new CommentModerationLog
            {
                CommentId = commentId,
                ModeratorId = moderatorId,
                Action = CommentModerationAction.Reinstated,
                Reason = null,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
    }
}
