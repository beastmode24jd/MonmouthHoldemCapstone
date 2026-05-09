using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.Services.Abstraction;
using System.Text;
using Moq;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class BadgeServiceTests
{
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;
    private Mock<IRepository<Badge, ApplicationDbContext>> _badgeRepoMock;
    private Mock<IRepository<UserBadge, ApplicationDbContext>> _userBadgeRepoMock;
    private Mock<INotificationService> _notificationServiceMock;
    private IBadgeService _badgeService;
    private Guid _testBadgeId;
    

    [SetUp]
    public void Setup()
    {
        _testBadgeId = Guid.NewGuid();
        // Add in the Mocked Repositories
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();
        _badgeRepoMock = new Mock<IRepository<Badge, ApplicationDbContext>>();
        _userBadgeRepoMock = new Mock<IRepository<UserBadge, ApplicationDbContext>>();
        _notificationServiceMock = new Mock<INotificationService>();

        _badgeService = new BadgeService(
            _badgeRepoMock.Object,
            _userBadgeRepoMock.Object,
            _userRepoMock.Object,
            _notificationServiceMock.Object
        );
    }

    [Test]
    public async Task AddBadge_ToValidUser_CallsReposAndUpdatesBadgesAndPoints()
    {
        // Arrange **********
        var userId = Guid.NewGuid();

        // LastLogin being 30 days or newer activates a Streak. Set it to 40.
        var user = new ApplicationUser{
            GuidId = userId,
            Points = 0,
            LastLogin = DateTimeOffset.UtcNow.AddDays(-40)};

        var badgeTemplate = new Badge{BadgeID = _testBadgeId, PointValue = 15, Title = "Test Badge"};

        var userBadge = new UserBadge{UserId = user.Id, User = user, Badge = badgeTemplate};

        _userBadgeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<UserBadge>().AsQueryable());

        _badgeRepoMock.Setup(r => r.FindByIdAsync(_testBadgeId))
                  .ReturnsAsync(badgeTemplate);

        _notificationServiceMock.Setup(s => s.SendNotificationAsync(
            It.IsAny<Notification>(), It.IsAny<NotificationType>()))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

        // Act ******************
        await _badgeService.AddBadge(user, _testBadgeId);

        // Assert ***************
        // Check that the Repos were called
        _badgeRepoMock.Verify(r => r.FindByIdAsync(_testBadgeId), Times.Once);

        // Check That UserBadge Id and UserId mock calls match with input values
        _userBadgeRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<UserBadge>(ub => 
        ub.BadgeId == _testBadgeId && ub.UserId == user.Id)), Times.Once);

        // Checks if UserBadge directory was called to save a new object (UserBadge)
        _userBadgeRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<UserBadge>(ub => 
        ub.UserId == user.Id && 
        ub.BadgeId == _testBadgeId)), Times.Once);

        // Test badge is worth 15 points, check to see if point increment is the same
        _userRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<ApplicationUser>(u => u.Points == 15)), Times.Once);

        // Verify notification was sent to the correct GuidId
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(
            It.Is<Notification>(n => n.RecipientId == user.GuidId), It.IsAny<NotificationType>()), Times.Once);

    }

    [Test]
    public async Task GetBadgeDetails_BadgeExists_ReturnsBadgeDetails()
    {
        // Arrange

        var testBadge = new Badge {
            BadgeID = _testBadgeId,
            Title = "Custom Profile Icon",
            PointValue = 10
        };

        // Set up the Mock to respond properly when called
        _badgeRepoMock.Setup(repo => repo.FindByIdAsync(_testBadgeId))
                  .ReturnsAsync(testBadge);
        
        // Act
        var searchBadge = await _badgeService.GetBadgeDetails(_testBadgeId);

        // Assert
        Assert.Multiple(() =>
        {
            // Check that the method found something...
            Assert.That(searchBadge, Is.Not.Null);

            // Then check that all the initialized details match.
            Assert.That(searchBadge!.BadgeID, Is.EqualTo(_testBadgeId));
            Assert.That(searchBadge!.Title, Is.EqualTo("Custom Profile Icon"));
            Assert.That(searchBadge!.PointValue, Is.EqualTo(10));
            _badgeRepoMock.Verify(r => r.FindByIdAsync(_testBadgeId), Times.Once);
        });
    }

    [Test]
    public async Task GetBadgeDetails_BadgeNotFound_ReturnsNull()
    {
        // Arrange
        Guid fakeId = Guid.NewGuid();

        // Guarantee that the Mock will return a null value for searching fakeId.
        _badgeRepoMock.Setup(r => r.FindByIdAsync(fakeId))
                    .ReturnsAsync((Badge?)null);

        // Act
        var searchBadge = await _badgeService.GetBadgeDetails(fakeId);

        // Assert
        Assert.That(searchBadge, Is.Null);
    }

    [Test]
    public async Task SortBadgesByTime_ValidBadgeList_ReturnsUserBadgeListDescending()
    {
        // Arrange
        // Add DateTime values to a UserBadge List.
        var oldTime = new DateTimeOffset(
            new DateTime(2001, 1, 1, 7, 0, 0),
            new TimeSpan(-7, 0, 0)
        );
        var newTime = DateTimeOffset.Now;

        var badgeList = new List<UserBadge>
        {
            new UserBadge { BadgeEarned = oldTime, UserBadgeId = Guid.NewGuid() },
            new UserBadge { BadgeEarned = newTime, UserBadgeId = Guid.NewGuid() }
        };

        // Act
        var result = await _badgeService.SortBadgesByTime(badgeList);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));

            // First item should be newest date.
            Assert.That(result[0].BadgeEarned, Is.EqualTo(newTime)); // newTime should be DateTimeOffset
            Assert.That(result[1].BadgeEarned, Is.EqualTo(oldTime)); // oldTime should be DateTimeOffset
        });
    }

    #region Multi-Step Badge Progression [CSP-184]

    public async Task UpdateBadgeProgress_ValidBadgeInput_UpdatesBadgeProgress()
    {
        
    }

    public async Task UpdateBadgeProgress_BadgeCompletedByAction_RewardsBadge()
    {
        
    }

    public async Task UpdateBadgeProgress_InvalidBadge_ThrowsException()
    {
        
    }

    #endregion
}