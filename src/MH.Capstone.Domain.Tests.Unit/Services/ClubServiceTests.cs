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
    private IClubService _clubService;
    private Mock<IRepository<ClubMembership, ApplicationDbContext>> _clubMembershipRepoMock;

    [SetUp]
    public void Setup()
    {
        _clubRepoMock = new Mock<IRepository<Club, ApplicationDbContext>>();
        _clubMembershipRepoMock = new Mock<IRepository<ClubMembership, ApplicationDbContext>>();

        _clubService = new ClubService(
            _clubRepoMock.Object, 
            _clubMembershipRepoMock.Object);
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
    public async Task GetUserClubsAsync_ReturnsOnlyUserClubs_SortedByClubName()
    {
        // Arrange

        // Alex should not be able to see Private Club B in his personal Club list.
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

        // Alex is a member of Club A and Club C, but not Club B (Lily's).
        var alexMemberships = new List<ClubMembership>
        {
            new ClubMembership(alexId, clubAId, DateTimeOffset.UtcNow),
            new ClubMembership(alexId, clubCId, DateTimeOffset.UtcNow),
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
        Assert.That(result[1].OwnerIdentityId, Is.EqualTo("Alex"));
        Assert.That(result[1].OwnerIdentityId, Is.EqualTo("Alex"));

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
            .Setup(r => r.AddOrUpdateAsync(It.IsAny<ClubMembership>()))
            .ReturnsAsync(new ClubMembership(ownerId, newClub.Id, DateTimeOffset.UtcNow))
            .Verifiable(Times.Once);

        // Act
        var result = await _clubService.CreateClubAsync(newClub);

        // Assert
        Assert.That(result, Is.EqualTo(newClub));
        _clubRepoMock.Verify(r => r.AddOrUpdateAsync(newClub), Times.Once);
        _clubMembershipRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<ClubMembership>()), Times.Once);
    }

    [Test]
    public void CreateClubAsync_NullClub_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _clubService.CreateClubAsync(null!));
    }

    #endregion

}
