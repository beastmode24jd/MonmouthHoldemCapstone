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
public class ClubServiceTests
{
    private Mock<IRepository<Club, ApplicationDbContext>> _clubRepoMock;
    private Mock<IRepository<ClubMembership, ApplicationDbContext>> _clubMembershipRepoMock;
    private Mock<IRepository<Message, ApplicationDbContext>> _messageRepoMock;
    private IClubService _clubService;

    [SetUp]
    public void Setup()
    {
        _clubRepoMock = new Mock<IRepository<Club, ApplicationDbContext>>();
        _clubMembershipRepoMock = new Mock<IRepository<ClubMembership, ApplicationDbContext>>();
        _messageRepoMock = new Mock<IRepository<Message, ApplicationDbContext>>();

        _clubService = new ClubService(
            _clubRepoMock.Object,
            _clubMembershipRepoMock.Object,
            _messageRepoMock.Object);
    }

    #region GetClubsMethods

    [Test]
    public async Task GetPublicClubsAsync_ReturnsOnlyPublicClubs()
    {
        // Arrange
        var clubs = new List<Club>
        {
            new Club { Name = "Public Club A",  IsPublic = true,  OwnerIdentityId = "owner1", CreatedAt = DateTimeOffset.UtcNow },
            new Club { Name = "Private Club B", IsPublic = false, OwnerIdentityId = "owner2", CreatedAt = DateTimeOffset.UtcNow },
        }.AsQueryable();

        _clubRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(clubs);

        // Act
        var result = (await _clubService.GetPublicClubsAsync()).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].IsPublic, Is.True);
        Assert.That(result[0].Name, Is.EqualTo("Public Club A"));
    }

    [Test]
    public async Task GetUserClubsAsync_ReturnsOnlyAcceptedMemberClubs_SortedByClubName()
    {
        // Arrange
        Guid alexId = Guid.NewGuid();
        var clubAId = Guid.NewGuid();
        var clubBId = Guid.NewGuid();
        var clubCId = Guid.NewGuid();

        var clubs = new List<Club>
        {
            new Club { Id = clubAId, Name = "Public Club A",  IsPublic = true,  OwnerIdentityId = "Alex", CreatedAt = DateTimeOffset.UtcNow },
            new Club { Id = clubBId, Name = "Private Club B", IsPublic = false, OwnerIdentityId = "Lily", CreatedAt = DateTimeOffset.UtcNow },
            new Club { Id = clubCId, Name = "Private Club C", IsPublic = false, OwnerIdentityId = "Alex", CreatedAt = DateTimeOffset.UtcNow },
        }.AsQueryable();

        // Alex has accepted memberships for Club A and Club C; Club B predicate returns nothing.
        var alexMemberships = new List<ClubMembership>
        {
            new ClubMembership(alexId, clubAId, DateTimeOffset.UtcNow) { AcceptedInvite = true },
            new ClubMembership(alexId, clubCId, DateTimeOffset.UtcNow) { AcceptedInvite = true },
        }.AsQueryable();

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(alexMemberships);

        _clubRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Club, bool>>>()))
            .ReturnsAsync(clubs.Where(c => c.Id == clubAId || c.Id == clubCId));

        // Act
        var result = (await _clubService.GetUserClubsAsync(alexId)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Private Club C"));
        Assert.That(result[1].Name, Is.EqualTo("Public Club A"));
    }

    [Test]
    public async Task GetPendingInvitesAsync_ReturnsPendingInviteClubs()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        var clubAId = Guid.NewGuid();
        var clubBId = Guid.NewGuid();

        var pendingMemberships = new List<ClubMembership>
        {
            new ClubMembership(userId, clubAId, DateTimeOffset.UtcNow) { AcceptedInvite = false },
        }.AsQueryable();

        var pendingClub = new Club { Id = clubAId, Name = "Bird Watchers", IsPublic = false, OwnerIdentityId = "other", CreatedAt = DateTimeOffset.UtcNow };

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(pendingMemberships);

        _clubRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Club, bool>>>()))
            .ReturnsAsync(new List<Club> { pendingClub }.AsQueryable());

        // Act
        var result = (await _clubService.GetPendingInvitesAsync(userId)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(clubAId));
    }

    [Test]
    public async Task GetPendingInvitesAsync_NoPendingInvites_ReturnsEmpty()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ClubMembership>().AsQueryable());

        // Act
        var result = (await _clubService.GetPendingInvitesAsync(userId)).ToList();

        // Assert
        Assert.That(result, Is.Empty);
        _clubRepoMock.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<Club, bool>>>()), Times.Never);
    }

    #endregion

    #region CreateClubAsync

    [Test]
    public async Task CreateClubAsync_ValidClub_SavesClubAndOwnerMembershipReturnsClub()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var newClub = new Club(ownerId, "Bird Watchers", "A club for birding enthusiasts", DateTimeOffset.UtcNow)
        {
            IsPublic = true
        };

        _clubRepoMock.Setup(r => r.AddOrUpdateAsync(newClub))
            .ReturnsAsync(newClub)
            .Verifiable(Times.Once);

        _clubMembershipRepoMock
            .Setup(r => r.AddOrUpdateAsync(It.Is<ClubMembership>(m => m.AcceptedInvite)))
            .ReturnsAsync(new ClubMembership(ownerId, newClub.Id, DateTimeOffset.UtcNow) { AcceptedInvite = true })
            .Verifiable(Times.Once);

        // Act
        var result = await _clubService.CreateClubAsync(newClub);

        // Assert
        Assert.That(result, Is.EqualTo(newClub));
        _clubRepoMock.Verify(r => r.AddOrUpdateAsync(newClub), Times.Once);
        _clubMembershipRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<ClubMembership>(m => m.AcceptedInvite)), Times.Once);
    }

    [Test]
    public void CreateClubAsync_NullClub_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _clubService.CreateClubAsync(null!));
    }

    #endregion

    #region ClubInviteMethods

    [Test]
    public async Task SendInviteAsync_ValidUsers_CreatesPendingMembership()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var club = new Club(senderId, "Bird Watchers", null, DateTimeOffset.UtcNow) { Id = clubId };

        // Sender is an accepted member; receiver has no membership row.
        var existingMemberships = new List<ClubMembership>
        {
            new ClubMembership(senderId, clubId, DateTimeOffset.UtcNow) { AcceptedInvite = true },
        }.AsQueryable();

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(existingMemberships);

        _clubMembershipRepoMock
            .Setup(r => r.AddOrUpdateAsync(It.Is<ClubMembership>(m => m.MemberId == receiverId && !m.AcceptedInvite)))
            .ReturnsAsync(new ClubMembership(receiverId, clubId, DateTimeOffset.UtcNow) { AcceptedInvite = false })
            .Verifiable(Times.Once);

        // Act
        await _clubService.SendInviteAsync(club, senderId, receiverId);

        // Assert
        _clubMembershipRepoMock.Verify(
            r => r.AddOrUpdateAsync(It.Is<ClubMembership>(m => m.MemberId == receiverId && !m.AcceptedInvite)),
            Times.Once);
    }

    [Test]
    public async Task SendInviteAsync_SenderNotMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var club = new Club { Id = clubId };

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ClubMembership>().AsQueryable());

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _clubService.SendInviteAsync(club, senderId, receiverId));
    }

    [Test]
    public async Task SendInviteAsync_ReceiverAlreadyMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var club = new Club { Id = clubId };

        var memberships = new List<ClubMembership>
        {
            new ClubMembership(senderId, clubId, DateTimeOffset.UtcNow) { AcceptedInvite = true },
            new ClubMembership(receiverId, clubId, DateTimeOffset.UtcNow) { AcceptedInvite = true },
        }.AsQueryable();

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(memberships);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _clubService.SendInviteAsync(club, senderId, receiverId));
    }

    [Test]
    public async Task AcceptInviteAsync_ValidPendingInvite_SetsAcceptedInviteTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var pendingMembership = new ClubMembership(userId, clubId, DateTimeOffset.UtcNow)
        {
            AcceptedInvite = false
        };

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(new List<ClubMembership> { pendingMembership }.AsQueryable());

        _clubMembershipRepoMock
            .Setup(r => r.AddOrUpdateAsync(It.IsAny<ClubMembership>()))
            .ReturnsAsync(pendingMembership)
            .Verifiable(Times.Once);

        // Act
        await _clubService.AcceptInviteAsync(clubId, userId);

        // Assert
        Assert.That(pendingMembership.AcceptedInvite, Is.True);
        _clubMembershipRepoMock.Verify(r => r.AddOrUpdateAsync(pendingMembership), Times.Once);
    }

    [Test]
    public void AcceptInviteAsync_NoPendingInvite_ThrowsInvalidOperationException()
    {
        // Arrange
        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ClubMembership>().AsQueryable());

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _clubService.AcceptInviteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public async Task DeclineInviteAsync_ValidPendingInvite_DeletesMembershipRow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var pendingMembership = new ClubMembership(userId, clubId, DateTimeOffset.UtcNow)
        {
            AcceptedInvite = false
        };

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(new List<ClubMembership> { pendingMembership }.AsQueryable());

        _clubMembershipRepoMock
            .Setup(r => r.DeleteAsync(pendingMembership))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

        // Act
        await _clubService.DeclineInviteAsync(clubId, userId);

        // Assert
        _clubMembershipRepoMock.Verify(r => r.DeleteAsync(pendingMembership), Times.Once);
    }

    [Test]
    public void DeclineInviteAsync_NoPendingInvite_ThrowsInvalidOperationException()
    {
        // Arrange
        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ClubMembership>().AsQueryable());

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _clubService.DeclineInviteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    #endregion

    #region LeaveClubAsync

    [Test]
    public async Task LeaveClubAsync_ValidMember_DeletesMembershipRow()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var club = new Club(ownerId, "Bird Watchers", null, DateTimeOffset.UtcNow) { Id = clubId };
        var membership = new ClubMembership(memberId, clubId, DateTimeOffset.UtcNow) { AcceptedInvite = true };

        _clubRepoMock
            .Setup(r => r.FindByIdAsync(clubId))
            .ReturnsAsync(club);

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(new List<ClubMembership> { membership }.AsQueryable());

        _clubMembershipRepoMock
            .Setup(r => r.DeleteAsync(membership))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once);

        // Act
        await _clubService.LeaveClubAsync(clubId, memberId);

        // Assert
        _clubMembershipRepoMock.Verify(r => r.DeleteAsync(membership), Times.Once);
    }

    [Test]
    public async Task LeaveClubAsync_UserIsOwner_ThrowsInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var club = new Club(ownerId, "Bird Watchers", null, DateTimeOffset.UtcNow) { Id = clubId };

        _clubRepoMock
            .Setup(r => r.FindByIdAsync(clubId))
            .ReturnsAsync(club);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _clubService.LeaveClubAsync(clubId, ownerId));
    }

    [Test]
    public async Task LeaveClubAsync_UserNotMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        var clubId = Guid.NewGuid();

        var club = new Club(ownerId, "Bird Watchers", null, DateTimeOffset.UtcNow) { Id = clubId };

        _clubRepoMock
            .Setup(r => r.FindByIdAsync(clubId))
            .ReturnsAsync(club);

        _clubMembershipRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<ClubMembership>().AsQueryable());

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _clubService.LeaveClubAsync(clubId, nonMemberId));
    }

    #endregion

    #region SendMessageAsync Tests

    [Test]
    public async Task SendMessageAsync_ValidInput_SavesMessageWithTrimmedContent()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var rawContent = "   Hello, this is a test message!   ";
        var expectedContent = "Hello, this is a test message!";

        _messageRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m)
            .Verifiable();

        // Act
        await _clubService.SendMessageAsync(clubId, senderId, rawContent);

        // Assert
        _messageRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<Message>(m =>
            m.ClubId == clubId &&
            m.AuthorId == senderId &&
            m.Content == expectedContent &&
            m.SentAt <= DateTimeOffset.UtcNow
        )), Times.Once);
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void SendMessageAsync_InvalidContent_ThrowsArgumentException(string? content)
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => 
            _clubService.SendMessageAsync(Guid.NewGuid(), Guid.NewGuid(), content!));
    }

    #endregion
    #region GetClubMessagesAsync Tests

    [Test]
    public async Task GetClubMessagesAsync_ReturnsOnlyMessagesForRequestedClub_OrderedByDate()
    {
        // Arrange
        var targetClubId = Guid.NewGuid();
        var otherClubId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var messages = new List<Message>
        {
            // Message 2: Target club, sent later
            new Message(targetClubId, Guid.NewGuid(), "Second Message", now.AddMinutes(10)),
            // Message 1: Target club, sent first
            new Message(targetClubId, Guid.NewGuid(), "First Message", now),
            // Message 3: Different club (should be filtered out)
            new Message(otherClubId, Guid.NewGuid(), "Wrong Club", now)
        }.AsQueryable();

        // Setup mock to return the list when GetAllAsync is called with an include expression
        _messageRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Message, object>>>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _clubService.GetClubMessagesAsync(targetClubId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should only return messages for the specified club.");
            Assert.That(result[0].Content, Is.EqualTo("First Message"), "Messages should be sorted by SentAt ascending.");
            Assert.That(result[1].Content, Is.EqualTo("Second Message"));
            Assert.That(result.All(m => m.ClubId == targetClubId), Is.True);
        });
    }

    [Test]
    public async Task GetClubMessagesAsync_NoMessagesFound_ReturnsEmptyList()
    {
        // Arrange
        _messageRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Message, object>>>()))
            .ReturnsAsync(Enumerable.Empty<Message>().AsQueryable());

        // Act
        var result = await _clubService.GetClubMessagesAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Empty);
    }
    #endregion
}
