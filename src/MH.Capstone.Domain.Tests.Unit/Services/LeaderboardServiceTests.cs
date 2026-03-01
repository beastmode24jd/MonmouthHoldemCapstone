using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.Domain.Tests.Unit.Services;

// Unit tests for LeaderboardService.

[TestFixture]
[Parallelizable(ParallelScope.Fixtures)]
[ExcludeFromCodeCoverage]
public class LeaderboardServiceTests
{
    // The in-memory stand-in for the real database, rebuilt fresh before every test.
    private ApplicationDbContext _dbContext = null!;

    // Runs before every single test method in this class.
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
    }

    // runs after every test to release the in-memory DB from memory.
    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }
   
    private LeaderboardService CreateSut() => new(_dbContext);

    
    private async Task SeedUserAsync(string id, string userName, int points)
    {
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpper(),
            Email = $"{userName}@test.com",
            NormalizedEmail = $"{userName.ToUpper()}@TEST.COM",
            Points = points,
            IsDeactivated = false
        });
        await _dbContext.SaveChangesAsync();
    }

    #region GetLeaderboardPageAsync — Ordering

    [Test]
    public async Task GetLeaderboardPageAsync_WithMultipleUsers_ReturnsUsersOrderedByPointsDescending()
    {
        // Arrange 
        await SeedUserAsync("user-1", "Alice", 100);   // should end up #3
        await SeedUserAsync("user-2", "Bob", 300);     // should end up #1
        await SeedUserAsync("user-3", "Charlie", 200); // should end up #2
        var sut = CreateSut();

        // Act 
        var result = await sut.GetLeaderboardPageAsync(page: 1);

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
        for (int i = 1; i <= 35; i++)
        {
            await SeedUserAsync($"user-{i}", $"User{i:D2}", points: i * 10);
        }
        var sut = CreateSut();

        // Act 
        var result = await sut.GetLeaderboardPageAsync(page: 1);

        // Assert 
        Assert.That(result, Has.Count.EqualTo(30));
    }

    [Test]
    public async Task GetLeaderboardPageAsync_SecondPageOf35Users_Returns5()
    {
        // Arrange 
        for (int i = 1; i <= 35; i++)
        {
            await SeedUserAsync($"user-{i}", $"User{i:D2}", points: i * 10);
        }
        var sut = CreateSut();

        // Act 
        var result = await sut.GetLeaderboardPageAsync(page: 2);

        // Assert
        Assert.That(result, Has.Count.EqualTo(5));
    }

    #endregion

    #region GetTotalUserCountAsync

    [Test]
    public async Task GetTotalUserCountAsync_WithThreeActiveUsers_Returns3()
    {
        // Arrange
        await SeedUserAsync("user-1", "Alice", 100);
        await SeedUserAsync("user-2", "Bob", 200);
        await SeedUserAsync("user-3", "Charlie", 300);
        var sut = CreateSut();

        // Act
        var result = await sut.GetTotalUserCountAsync();

        // Assert
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task GetTotalUserCountAsync_DeactivatedUsersAreExcluded()
    {
        // Arrange 
        await SeedUserAsync("user-1", "ActiveUser", 100);
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = "user-2",
            UserName = "DeactivatedUser",
            NormalizedUserName = "DEACTIVATEDUSER",
            Email = "deactivated@test.com",
            NormalizedEmail = "DEACTIVATED@TEST.COM",
            Points = 9999,
            IsDeactivated = true   
        });
        await _dbContext.SaveChangesAsync();
        var sut = CreateSut();

        // Act
        var result = await sut.GetTotalUserCountAsync();

        // Assert 
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion
}