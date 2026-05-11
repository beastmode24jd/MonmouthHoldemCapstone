using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using Moq;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class CommentServiceTests
{
    private Mock<IRepository<Comment, ApplicationDbContext>> _commentRepoMock = null!;
    private Mock<IRepository<CommentModerationLog, ApplicationDbContext>> _logRepoMock = null!;
    private Mock<IRepository<Sighting, ApplicationDbContext>> _sightingRepoMock = null!;
    private Mock<IBlockService> _blockServiceMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private ICommentService _commentService = null!;

    private Guid _alexId;
    private Guid _lilyId;
    private Guid _patriciaId; // moderator
    private Guid _sightingId;
    private Sighting _sighting = null!;
    private ApplicationUser _alex = null!;

    [SetUp]
    public void Setup()
    {
        _commentRepoMock = new Mock<IRepository<Comment, ApplicationDbContext>>();
        _logRepoMock = new Mock<IRepository<CommentModerationLog, ApplicationDbContext>>();
        _sightingRepoMock = new Mock<IRepository<Sighting, ApplicationDbContext>>();
        _blockServiceMock = new Mock<IBlockService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _userServiceMock = new Mock<IUserService>();

        _alexId = Guid.NewGuid();
        _lilyId = Guid.NewGuid();
        _patriciaId = Guid.NewGuid();
        _sightingId = Guid.NewGuid();

        _alex = new ApplicationUser { GuidId = _alexId, DisplayName = "Alex" };
        // Sighting owned by Lily
        _sighting = new Sighting
        {
            Id = _sightingId,
            UserIdentityId = _lilyId.ToString(),
            SpeciesName = "Wolverine",
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1),
            ImageBuffer = new byte[] { 0x01 },
        };

        _sightingRepoMock.Setup(r => r.FindByIdAsync(_sightingId)).ReturnsAsync(_sighting);
        _userServiceMock.Setup(u => u.GetUserByIdAsync(_alexId)).ReturnsAsync(_alex);

        // Echo persisted comment back so callers receive the saved instance.
        _commentRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Comment>()))
            .ReturnsAsync((Comment c) => c);

        SetExistingComments();
        SetBlockedUserIds();

        _commentService = new CommentService(
            _commentRepoMock.Object,
            _logRepoMock.Object,
            _sightingRepoMock.Object,
            _blockServiceMock.Object,
            _notificationServiceMock.Object,
            _userServiceMock.Object);
    }

    private void SetExistingComments(params Comment[] comments)
    {
        _commentRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Comment, bool>>>()))
            .ReturnsAsync((Expression<Func<Comment, bool>> pred) => comments.AsQueryable().Where(pred));
    }

    private void SetBlockedUserIds(params Guid[] ids)
    {
        _blockServiceMock.Setup(b => b.GetBlockedUserIdsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(ids.ToList());
    }

    [Test]
    public async Task PostCommentAsync_NewComment_PersistsAndNotifiesOwner()
    {
        var result = await _commentService.PostCommentAsync(_sightingId, _alexId, "Cool wolverine!");

        _commentRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<Comment>(c =>
            c.SightingId == _sightingId &&
            c.AuthorIdentityId == _alexId.ToString() &&
            c.Body == "Cool wolverine!")), Times.Once);

        _notificationServiceMock.Verify(s => s.SendNotificationAsync(
            It.Is<Notification>(n => n.RecipientId == _lilyId),
            NotificationType.NewComment), Times.Once);

        Assert.That(result.Body, Is.EqualTo("Cool wolverine!"));
    }

    [Test]
    public void PostCommentAsync_EmptyBody_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _commentService.PostCommentAsync(_sightingId, _alexId, "   "));
    }

    [Test]
    public void PostCommentAsync_NoSighting_Throws()
    {
        var unknown = Guid.NewGuid();
        _sightingRepoMock.Setup(r => r.FindByIdAsync(unknown)).ReturnsAsync((Sighting?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _commentService.PostCommentAsync(unknown, _alexId, "hi"));
    }

    [Test]
    public async Task PostCommentAsync_AuthorIsOwner_DoesNotSelfNotify()
    {
        var ownComment = "self-comment";

        await _commentService.PostCommentAsync(_sightingId, _lilyId, ownComment);

        _commentRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Comment>()), Times.Once);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(
            It.IsAny<Notification>(), It.IsAny<NotificationType>()), Times.Never);
    }

    [Test]
    public void PostCommentAsync_FiveInLastMinute_ThrowsRateLimit()
    {
        var now = DateTimeOffset.UtcNow;
        var recent = Enumerable.Range(0, 5).Select(i => new Comment
        {
            Id = Guid.NewGuid(),
            SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(),
            Body = $"c{i}",
            CreatedAt = now.AddSeconds(-i * 5),
        }).ToArray();
        SetExistingComments(recent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _commentService.PostCommentAsync(_sightingId, _alexId, "one more"));

        Assert.That(ex!.Message, Does.Contain("Rate limit"));
    }

    [Test]
    public async Task PostCommentAsync_OldComments_DoNotCountTowardRateLimit()
    {
        // 5 comments from over a minute ago: should NOT trip the limit.
        var old = Enumerable.Range(0, 5).Select(i => new Comment
        {
            Id = Guid.NewGuid(),
            SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(),
            Body = $"old{i}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        }).ToArray();
        SetExistingComments(old);

        await _commentService.PostCommentAsync(_sightingId, _alexId, "fresh");

        _commentRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Comment>()), Times.Once);
    }

    [Test]
    public async Task GetCommentsForSightingAsync_FiltersHiddenAndBlocked_OldestFirst()
    {
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);
        var visible1 = new Comment
        {
            Id = Guid.NewGuid(), SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "first", CreatedAt = t0,
        };
        var hidden = new Comment
        {
            Id = Guid.NewGuid(), SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "removed", CreatedAt = t0.AddMinutes(1),
            IsHidden = true,
        };
        var fromBlocked = new Comment
        {
            Id = Guid.NewGuid(), SightingId = _sightingId,
            AuthorIdentityId = _lilyId.ToString(), Body = "blocked", CreatedAt = t0.AddMinutes(2),
        };
        var visible2 = new Comment
        {
            Id = Guid.NewGuid(), SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "second", CreatedAt = t0.AddMinutes(3),
        };
        SetExistingComments(visible1, hidden, fromBlocked, visible2);
        SetBlockedUserIds(_lilyId);

        var result = (await _commentService.GetCommentsForSightingAsync(_sightingId, viewerId: _patriciaId)).ToList();

        Assert.That(result.Select(c => c.Body), Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public async Task GetCommentsForSightingAsync_NoViewer_StillExcludesHidden()
    {
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-5);
        var visible = new Comment
        {
            Id = Guid.NewGuid(), SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "v", CreatedAt = t0,
        };
        var hidden = new Comment
        {
            Id = Guid.NewGuid(), SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "h", CreatedAt = t0.AddMinutes(1),
            IsHidden = true,
        };
        SetExistingComments(visible, hidden);

        var result = (await _commentService.GetCommentsForSightingAsync(_sightingId)).ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Body, Is.EqualTo("v"));
        _blockServiceMock.Verify(b => b.GetBlockedUserIdsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task HideAsync_VisibleComment_SetsFlagAndLogs()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id = commentId, SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "bad", CreatedAt = DateTimeOffset.UtcNow,
        };
        _commentRepoMock.Setup(r => r.FindByIdAsync(commentId)).ReturnsAsync(comment);

        await _commentService.HideAsync(commentId, _patriciaId, "spam");

        _commentRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<Comment>(c =>
            c.Id == commentId &&
            c.IsHidden == true &&
            c.HiddenByIdentityId == _patriciaId.ToString() &&
            c.HiddenReason == "spam" &&
            c.HiddenAt != null)), Times.Once);

        _logRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<CommentModerationLog>(l =>
            l.CommentId == commentId &&
            l.ModeratorIdentityId == _patriciaId.ToString() &&
            l.Action == CommentModerationAction.Hidden &&
            l.Reason == "spam")), Times.Once);
    }

    [Test]
    public async Task HideAsync_AlreadyHidden_IsNoOp()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id = commentId, SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "bad", CreatedAt = DateTimeOffset.UtcNow,
            IsHidden = true,
        };
        _commentRepoMock.Setup(r => r.FindByIdAsync(commentId)).ReturnsAsync(comment);

        await _commentService.HideAsync(commentId, _patriciaId, "again");

        _commentRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Comment>()), Times.Never);
        _logRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<CommentModerationLog>()), Times.Never);
    }

    [Test]
    public async Task ReinstateAsync_HiddenComment_ClearsFlagAndLogs()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id = commentId, SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "ok", CreatedAt = DateTimeOffset.UtcNow,
            IsHidden = true, HiddenAt = DateTimeOffset.UtcNow,
            HiddenByIdentityId = _patriciaId.ToString(), HiddenReason = "old reason",
        };
        _commentRepoMock.Setup(r => r.FindByIdAsync(commentId)).ReturnsAsync(comment);

        await _commentService.ReinstateAsync(commentId, _patriciaId);

        _commentRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<Comment>(c =>
            c.Id == commentId &&
            c.IsHidden == false &&
            c.HiddenAt == null &&
            c.HiddenByIdentityId == null &&
            c.HiddenReason == null)), Times.Once);

        _logRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<CommentModerationLog>(l =>
            l.CommentId == commentId &&
            l.ModeratorIdentityId == _patriciaId.ToString() &&
            l.Action == CommentModerationAction.Reinstated)), Times.Once);
    }

    [Test]
    public async Task ReinstateAsync_NotHidden_IsNoOp()
    {
        var commentId = Guid.NewGuid();
        var comment = new Comment
        {
            Id = commentId, SightingId = _sightingId,
            AuthorIdentityId = _alexId.ToString(), Body = "ok", CreatedAt = DateTimeOffset.UtcNow,
            IsHidden = false,
        };
        _commentRepoMock.Setup(r => r.FindByIdAsync(commentId)).ReturnsAsync(comment);

        await _commentService.ReinstateAsync(commentId, _patriciaId);

        _commentRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Comment>()), Times.Never);
        _logRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<CommentModerationLog>()), Times.Never);
    }
}
