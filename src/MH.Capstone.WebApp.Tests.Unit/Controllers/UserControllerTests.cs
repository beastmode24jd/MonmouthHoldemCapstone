// Sprint 5, CSP-54
using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
[ExcludeFromCodeCoverage]
public class UserControllerTests
{
    private Mock<IUserService> _mockUserService = null!;
    private Mock<ILogger<UserController>> _mockLogger = null!;
    private UserController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UserController>>();
        _controller = new UserController(_mockUserService.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown() => _controller?.Dispose();

    #region Search

    [Test]
    public void Search_ReturnsViewResult()
    {
        // Act
        var result = _controller.Search();

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    #endregion

    #region SearchResults

    [Test]
    public async Task SearchResults_EmptyQuery_ReturnsOkWithEmptyResponse()
    {
        // Act
        var result = await _controller.SearchResults(null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var dto = result!.Value as UserSearchResponseDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.TotalCount, Is.EqualTo(0));
        Assert.That(dto.Results, Is.Empty);
    }

    [Test]
    public async Task SearchResults_WhitespaceQuery_ReturnsOkWithEmptyResponse()
    {
        // Act
        var result = await _controller.SearchResults("   ") as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var dto = result!.Value as UserSearchResponseDto;
        Assert.That(dto!.TotalCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SearchResults_ValidQuery_ReturnsOkWithMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid().ToString(), UserName = "alice@test.com" },
            new() { Id = Guid.NewGuid().ToString(), UserName = "alice_smith@test.com" }
        };
        _mockUserService
            .Setup(s => s.SearchUsersAsync("alice"))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.SearchResults("alice") as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var dto = result!.Value as UserSearchResponseDto;
        Assert.That(dto!.TotalCount, Is.EqualTo(2));
        Assert.That(dto.Results.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task SearchResults_MoreThan10Results_ReturnsFirstPageOf10()
    {
        // Arrange
        var users = Enumerable.Range(1, 15).Select(i => new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"user{i:D2}@test.com"
        }).ToList();

        _mockUserService
            .Setup(s => s.SearchUsersAsync("user"))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.SearchResults("user", page: 1) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var dto = result!.Value as UserSearchResponseDto;
        Assert.That(dto!.TotalCount, Is.EqualTo(15));
        Assert.That(dto.Results.Count(), Is.EqualTo(10));
        Assert.That(dto.TotalPages, Is.EqualTo(2));
        Assert.That(dto.Page, Is.EqualTo(1));
    }

    [Test]
    public async Task SearchResults_Page2_ReturnsCorrectSlice()
    {
        // Arrange
        var users = Enumerable.Range(1, 15).Select(i => new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"user{i:D2}@test.com"
        }).ToList();

        _mockUserService
            .Setup(s => s.SearchUsersAsync("user"))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.SearchResults("user", page: 2) as OkObjectResult;

        // Assert
        var dto = result!.Value as UserSearchResponseDto;
        Assert.That(dto!.Results.Count(), Is.EqualTo(5));
        Assert.That(dto.Page, Is.EqualTo(2));
    }

    [Test]
    public async Task SearchResults_NoMatches_ReturnsOkWithZeroCount()
    {
        // Arrange
        _mockUserService
            .Setup(s => s.SearchUsersAsync("xyzzy"))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        var result = await _controller.SearchResults("xyzzy") as OkObjectResult;

        // Assert
        var dto = result!.Value as UserSearchResponseDto;
        Assert.That(dto!.TotalCount, Is.EqualTo(0));
        Assert.That(dto.Results, Is.Empty);
    }

    #endregion

    #region ProfileImage

    [Test]
    public async Task ProfileImage_UserHasNoProfileImage_RedirectsToDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId.ToString(), ProfileImage = null };
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _controller.ProfileImage(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectResult>());
        var redirect = (RedirectResult)result;
        Assert.That(redirect.Url, Is.EqualTo("/imgs/profileDefault.jpg"));
    }

    [Test]
    public async Task ProfileImage_UserNotFound_RedirectsToDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.ProfileImage(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectResult>());
    }

    [Test]
    public async Task ProfileImage_UserHasProfileImage_ReturnsFileResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId.ToString(),
            ProfileImage = new byte[] { 0x01, 0x02 },
            ProfileImageType = "image/png"
        };
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _controller.ProfileImage(userId);

        // Assert
        Assert.That(result, Is.InstanceOf<FileContentResult>());
        var file = (FileContentResult)result;
        Assert.That(file.ContentType, Is.EqualTo("image/png"));
    }

    #endregion
}
