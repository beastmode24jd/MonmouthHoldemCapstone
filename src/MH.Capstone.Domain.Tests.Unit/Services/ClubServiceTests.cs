using System.Diagnostics.CodeAnalysis;
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

    [SetUp]
    public void Setup()
    {
        _clubRepoMock = new Mock<IRepository<Club, ApplicationDbContext>>();
        _clubService = new ClubService(_clubRepoMock.Object);
    }

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
}
