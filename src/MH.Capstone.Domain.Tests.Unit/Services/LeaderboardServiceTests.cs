using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace MH.Capstone.Domain.Tests.Unit.Services;

// Unit tests for LeaderboardService.

[TestFixture]
[Parallelizable(ParallelScope.Fixtures)]
[ExcludeFromCodeCoverage]
public class LeaderboardServiceTests
{
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock = null!;

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();
    }

    private LeaderboardService CreateSut() =>
        new(_userRepoMock.Object, NullLogger<LeaderboardService>.Instance);

    /// <summary>
    /// Configures the mock repository to return the given users when queried with any predicate.
    /// In unit tests we supply pre-filtered data directly; testing the actual predicate logic
    /// belongs to repository/integration tests.
    /// </summary>
    private void SetupUserRepoMock(IEnumerable<ApplicationUser> users)
    {
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(users.AsQueryable());
    }

    private static ApplicationUser MakeUser(string id, string userName, int points, bool isDeactivated = false) =>
        new()
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpper(),
            Email = $"{userName}@test.com",
            NormalizedEmail = $"{userName.ToUpper()}@TEST.COM",
            Points = points,
            IsDeactivated = isDeactivated
        };

    #region GetLeaderboardPageAsync — Ordering

    [Test]
    public async Task GetLeaderboardPageAsync_WithMultipleUsers_ReturnsUsersOrderedByPointsDescending()
    {
        // Arrange
        SetupUserRepoMock([
            MakeUser("user-1", "Alice", 100),   // should end up #3
            MakeUser("user-2", "Bob", 300),     // should end up #1
            MakeUser("user-3", "Charlie", 200)  // should end up #2
        ]);
        var sut = CreateSut();

        // Act
        var result = (await sut.GetLeaderboardPageAsync(page: 1)).ToList();

        // Assert
        Assert.That(result[0].UserName, Is.EqualTo("Bob"));
        Assert.That(result[1].UserName, Is.EqualTo("Charlie"));
        Assert.That(result[2].UserName, Is.EqualTo("Alice"));
    }

    #endregion

    #region GetLeaderboardPageAsync — Pagination

    [Test]
    public async Task GetLeaderboardPageAsync_FirstPageOf35Users_Returns30()
    {
        // Arrange
        SetupUserRepoMock(
            Enumerable.Range(1, 35).Select(i => MakeUser($"user-{i}", $"User{i:D2}", i * 10))
        );
        var sut = CreateSut();

        // Act
        var result = (await sut.GetLeaderboardPageAsync(page: 1)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(30));
    }

    [Test]
    public async Task GetLeaderboardPageAsync_SecondPageOf35Users_Returns5()
    {
        // Arrange
        SetupUserRepoMock(
            Enumerable.Range(1, 35).Select(i => MakeUser($"user-{i}", $"User{i:D2}", i * 10))
        );
        var sut = CreateSut();

        // Act
        var result = (await sut.GetLeaderboardPageAsync(page: 2)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(5));
    }

    #endregion

    #region GetUserRankAsync

    [Test]
    public async Task GetUserRankAsync_HighestScoringUser_Returns1()
    {
        // Arrange
        SetupUserRepoMock([
            MakeUser("user-1", "Alice", 100),
            MakeUser("user-2", "Bob", 300),   // highest (should be rank 1)
            MakeUser("user-3", "Charlie", 200)
        ]);
        var sut = CreateSut();

        // Act
        var rank = await sut.GetUserRankAsync("user-2");

        // Assert
        Assert.That(rank, Is.EqualTo(1));
    }

    [Test]
    public async Task GetUserRankAsync_LowestScoringUser_Returns3()
    {
        // Arrange
        SetupUserRepoMock([
            MakeUser("user-1", "Alice", 100),   // lowest (should be rank 3)
            MakeUser("user-2", "Bob", 300),
            MakeUser("user-3", "Charlie", 200)
        ]);
        var sut = CreateSut();

        // Act
        var rank = await sut.GetUserRankAsync("user-1");

        // Assert
        Assert.That(rank, Is.EqualTo(3));
    }

    [Test]
    public async Task GetUserRankAsync_UnknownUserId_Returns0()
    {
        // Arrange
        SetupUserRepoMock([
            MakeUser("user-1", "Alice", 100)
        ]);
        var sut = CreateSut();

        // Act
        var rank = await sut.GetUserRankAsync("does-not-exist");

        // Assert
        Assert.That(rank, Is.EqualTo(0)); // 0 is the "not found" sentinel value for this method.
    }

    #endregion
}