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
public class FollowServiceTests
{
    private Mock<IRepository<UserFollow, ApplicationDbContext>> _followRepoMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private IFollowService _followService = null!;

    private Guid _alexId;
    private Guid _lilyId;
    private ApplicationUser _alex = null!;
    private ApplicationUser _lily = null!;

    [SetUp]
    public void Setup()
    {
        _followRepoMock = new Mock<IRepository<UserFollow, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _userServiceMock = new Mock<IUserService>();

        _alexId = Guid.NewGuid();
        _lilyId = Guid.NewGuid();
        _alex = new ApplicationUser { GuidId = _alexId, DisplayName = "Alex" };
        _lily = new ApplicationUser { GuidId = _lilyId, DisplayName = "Lily" };

        _userServiceMock.Setup(u => u.GetUserByIdAsync(_alexId)).ReturnsAsync(_alex);
        _userServiceMock.Setup(u => u.GetUserByIdAsync(_lilyId)).ReturnsAsync(_lily);

        _followService = new FollowService(
            _followRepoMock.Object,
            _notificationServiceMock.Object,
            _userServiceMock.Object);
    }

    private void SetExistingFollows(params UserFollow[] follows)
    {
        _followRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<UserFollow, bool>>>()))
            .ReturnsAsync((Expression<Func<UserFollow, bool>> pred) => follows.AsQueryable().Where(pred));
    }

    [Test]
    public async Task FollowAsync_NewPair_PersistsRowAndSendsNotification()
    {
        SetExistingFollows();

        await _followService.FollowAsync(_alexId, _lilyId);

        _followRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<UserFollow>(f =>
            f.FollowerIdentityId == _alexId.ToString() &&
            f.FolloweeIdentityId == _lilyId.ToString())), Times.Once);

        _notificationServiceMock.Verify(s => s.SendNotificationAsync(
            It.Is<Notification>(n => n.RecipientId == _lilyId),
            NotificationType.NewFollower), Times.Once);
    }

    [Test]
    public async Task FollowAsync_AlreadyFollowing_IsNoOp()
    {
        SetExistingFollows(new UserFollow(_alexId, _lilyId));

        await _followService.FollowAsync(_alexId, _lilyId);

        _followRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<UserFollow>()), Times.Never);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(
            It.IsAny<Notification>(), It.IsAny<NotificationType>()), Times.Never);
    }

    [Test]
    public void FollowAsync_FollowSelf_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _followService.FollowAsync(_alexId, _alexId));
    }

    [Test]
    public async Task UnfollowAsync_ExistingFollow_DeletesRow()
    {
        var existing = new UserFollow(_alexId, _lilyId);
        SetExistingFollows(existing);

        await _followService.UnfollowAsync(_alexId, _lilyId);

        _followRepoMock.Verify(r => r.DeleteAsync(existing), Times.Once);
    }

    [Test]
    public async Task UnfollowAsync_NotFollowing_IsNoOp()
    {
        SetExistingFollows();

        await _followService.UnfollowAsync(_alexId, _lilyId);

        _followRepoMock.Verify(r => r.DeleteAsync(It.IsAny<UserFollow>()), Times.Never);
    }

    [Test]
    public async Task IsFollowingAsync_FollowExists_ReturnsTrue()
    {
        SetExistingFollows(new UserFollow(_alexId, _lilyId));

        var result = await _followService.IsFollowingAsync(_alexId, _lilyId);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsFollowingAsync_NoFollow_ReturnsFalse()
    {
        SetExistingFollows();

        var result = await _followService.IsFollowingAsync(_alexId, _lilyId);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetFolloweeIdsAsync_ReturnsAllFolloweeGuids()
    {
        var third = Guid.NewGuid();
        SetExistingFollows(
            new UserFollow(_alexId, _lilyId),
            new UserFollow(_alexId, third),
            new UserFollow(_lilyId, _alexId)); // not Alex's

        var result = (await _followService.GetFolloweeIdsAsync(_alexId)).ToList();

        Assert.That(result, Is.EquivalentTo(new[] { _lilyId, third }));
    }

    // [CSP-211] Inbound-direction lookup powers the follower count + tab list.

    [Test]
    public async Task GetFollowerIdsAsync_ReturnsAllInboundFollowerGuids()
    {
        var third = Guid.NewGuid();
        SetExistingFollows(
            new UserFollow(_lilyId, _alexId),  // Lily follows Alex
            new UserFollow(third,   _alexId),  // third follows Alex
            new UserFollow(_alexId, _lilyId)); // outbound from Alex — should NOT be returned

        var result = (await _followService.GetFollowerIdsAsync(_alexId)).ToList();

        Assert.That(result, Is.EquivalentTo(new[] { _lilyId, third }));
    }

    [Test]
    public async Task GetFollowerIdsAsync_NoFollowers_ReturnsEmpty()
    {
        SetExistingFollows();

        var result = (await _followService.GetFollowerIdsAsync(_alexId)).ToList();

        Assert.That(result, Is.Empty);
    }
}
