using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
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
    private LeaderboardController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLeaderboardService = new Mock<ILeaderboardService>();

        _controller = new LeaderboardController(_mockLeaderboardService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    // Helper — simulates a logged-in user with a given ID attached to the controller's HttpContext.
    private void SetLoggedInUser(string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // Helper — simulates no logged-in user (anonymous visitor).
    private void SetAnonymousUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Index — ViewModel construction

    [Test]
    public async Task Index_ReturnsViewWithLeaderboardViewModel()
    {
        // Arrange — service returns 3 users, total count is 3.
        var fakeUsers = new List<ApplicationUser>
        {
            new() { Id = "user-1", UserName = "Alice", Points = 300 },
            new() { Id = "user-2", UserName = "Bob",   Points = 200 },
            new() { Id = "user-3", UserName = "Charlie", Points = 100 }
        };
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(fakeUsers);
        _mockLeaderboardService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(3);
        SetAnonymousUser();

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;

        // Assert — result is a view with a LeaderboardViewModel as its model.
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.InstanceOf<LeaderboardViewModel>());
    }

    [Test]
    public async Task Index_AnonymousUser_ViewModelHasNullCurrentUserId()
    {
        // Arrange
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(new List<ApplicationUser>());
        _mockLeaderboardService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(0);
        SetAnonymousUser();

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;
        var vm = result!.Model as LeaderboardViewModel;

        // Assert — no user logged in means no highlight should appear.
        Assert.That(vm!.CurrentUserId, Is.Null);
    }

    [Test]
    public async Task Index_LoggedInUser_ViewModelContainsCurrentUserId()
    {
        // Arrange
        _mockLeaderboardService.Setup(s => s.GetLeaderboardPageAsync(1, 30)).ReturnsAsync(new List<ApplicationUser>());
        _mockLeaderboardService.Setup(s => s.GetTotalUserCountAsync()).ReturnsAsync(0);
        _mockLeaderboardService.Setup(s => s.GetUserRankAsync("user-1")).ReturnsAsync(1);
        SetLoggedInUser("user-1");

        // Act
        var result = await _controller.Index(page: 1) as ViewResult;
        var vm = result!.Model as LeaderboardViewModel;

        // Assert — the ViewModel carries the logged-in user's ID so the view can highlight their row.
        Assert.That(vm!.CurrentUserId, Is.EqualTo("user-1"));
    }

    #endregion
}