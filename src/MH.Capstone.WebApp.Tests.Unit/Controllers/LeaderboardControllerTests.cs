using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
[ExcludeFromCodeCoverage]
public class LeaderboardControllerTests
{
    private Mock<ILeaderboardService> _mockLeaderboardService = null!;
    private Mock<IUserService> _mockUserService = null!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private LeaderboardController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLeaderboardService = new Mock<ILeaderboardService>();
        _mockUserService = new Mock<IUserService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new LeaderboardController(_mockLeaderboardService.Object, _mockUserService.Object, _userManagerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    // simulates a logged-in user: mocks UserManager.GetUserId to return the given ID.
    private void SetLoggedInUser(string userId)
    {
        _userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // simulates a visitor who is not logged in: UserManager.GetUserId returns null.
    private void SetAnonymousUser()
    {
        _userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((string?)null);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Index — ViewModel construction

    [Test]
    public async Task Index_ReturnsViewWithLeaderboardViewModel()
    {
        // Arrange
        var fakeUsers = new List<ApplicationUser>
        {
            new() { Id = "user-1", DisplayName = "Alice", Points = 300 },
            new() { Id = "user-2", DisplayName = "Bob",   Points = 200 },
            new() { Id = "user-3", DisplayName = "Charlie", Points = 100 }
        };
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(fakeUsers);
        _mockUserService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(3);
        SetAnonymousUser();

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.InstanceOf<LeaderboardViewModel>());
    }

    [Test]
    public async Task Index_AnonymousUser_ViewModelHasNullCurrentUserId()
    {
        // Arrange
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(new List<ApplicationUser>());
        _mockUserService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(0);
        SetAnonymousUser();

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;
        var vm = result!.Model as LeaderboardViewModel;

        // Assert
        Assert.That(vm!.CurrentUserId, Is.Null);
    }

    [Test]
    public async Task Index_LoggedInUser_ViewModelContainsCurrentUserId()
    {
        // Arrange
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(new List<ApplicationUser>());
        _mockUserService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(0);
        _mockLeaderboardService.Setup(s => s.GetUserRankAsync("user-1")).ReturnsAsync(1);
        SetLoggedInUser("user-1");

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;
        var vm = result!.Model as LeaderboardViewModel;

        // Assert
        Assert.That(vm!.CurrentUserId, Is.EqualTo("user-1"));
    }

    #endregion

    #region CSP-170 — Leaderboard entry projection (no email in public view model)

    [Test]
    public async Task Index_UsersProjectedToLeaderboardEntryViewModels()
    {
        // Arrange
        var fakeUsers = new List<ApplicationUser>
        {
            new() { Id = "user-1", DisplayName = "Alice", Email = "alice@test.com", Points = 300 },
            new() { Id = "user-2", DisplayName = "Bob",   Email = "bob@test.com",   Points = 200 },
        };
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(fakeUsers);
        _mockUserService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(2);
        SetAnonymousUser();

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;
        var vm = result!.Model as LeaderboardViewModel;

        // Assert — entries are the DTO type, not the full entity
        Assert.That(vm!.Users, Is.All.InstanceOf<LeaderboardEntryViewModel>());
    }

    [Test]
    public async Task Index_LeaderboardEntries_ContainDisplayNameNotEmail()
    {
        // Arrange
        var fakeUsers = new List<ApplicationUser>
        {
            new() { Id = "user-1", DisplayName = "Alice", Email = "alice@test.com", Points = 300 },
        };
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(fakeUsers);
        _mockUserService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(1);
        SetAnonymousUser();

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;
        var vm = result!.Model as LeaderboardViewModel;
        var entry = vm!.Users.Single();

        // Assert — DisplayName is projected, email-containing strings are absent
        Assert.That(entry.DisplayName, Is.EqualTo("Alice"));
        Assert.That(entry.DisplayName, Does.Not.Contain("@"),
            "leaderboard entry must not expose email address");
    }

    [Test]
    public async Task Index_LeaderboardEntries_PreserveIdAndPoints()
    {
        // Arrange
        var fakeUsers = new List<ApplicationUser>
        {
            new() { Id = "user-1", DisplayName = "Alice", Email = "alice@test.com", Points = 300 },
        };
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(fakeUsers);
        _mockUserService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(1);
        SetAnonymousUser();

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;
        var vm = result!.Model as LeaderboardViewModel;
        var entry = vm!.Users.Single();

        Assert.That(entry.Id, Is.EqualTo("user-1"));
        Assert.That(entry.Points, Is.EqualTo(300));
    }

    #endregion
}