using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
[ExcludeFromCodeCoverage]
public class SightingControllerTests
{
    private Mock<ILogger<SightingController>> _mockLogger = null!;
    private Mock<ISightingsService> _mockSightingsService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<IBadgeService> _mockBadgeService = null!;
    private SightingController _controller = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly ApplicationUser TestUser = new()
    {
        Id = TestUserId,
        UserName = "testuser@example.com",
        Email = "testuser@example.com"
    };

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<SightingController>>();
        _mockSightingsService = new Mock<ISightingsService>();
        _mockBadgeService = new Mock<IBadgeService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new SightingController(
            _mockLogger.Object,
            _mockSightingsService.Object,
            _mockUserManager.Object,
            _mockBadgeService.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, TestUserId)
                }, "mock"))
            }
        };

        _mockUserManager
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(TestUser);
    }

    [TearDown]
    public void TearDown() => _controller?.Dispose();

    #region CSP-96: Gallery action tests

    [Test]
    public async Task Gallery_ReturnsViewResult_WithSightingGalleryViewModel()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetAllSightingsAsync())
            .ReturnsAsync(new List<Sighting>());

        // Act
        var result = await _controller.Gallery() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.InstanceOf<SightingGalleryViewModel>());
    }

    [Test]
    public async Task Gallery_CallsGetAllSightingsAsync_NotGetUserSightings()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetAllSightingsAsync())
            .ReturnsAsync(new List<Sighting>());

        // Act
        await _controller.Gallery();

        // Assert: GetAllSightingsAsync was called once
        _mockSightingsService.Verify(s => s.GetAllSightingsAsync(), Times.Once);
        // GetUserSightingsAsync should NOT be called
        _mockSightingsService.Verify(s => s.GetUserSightingsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Gallery_SetsCurrentUserIdOnViewModel()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetAllSightingsAsync())
            .ReturnsAsync(new List<Sighting>());

        // Act
        var result = await _controller.Gallery() as ViewResult;
        var vm = result!.Model as SightingGalleryViewModel;

        // Assert
        Assert.That(vm!.CurrentUserId, Is.EqualTo(TestUserId));
    }

    [Test]
    public async Task Gallery_WithSightingsFromMultipleUsers_AllIncludedInViewModel()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user1@test.com" };
        var user2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2@test.com" };

        var sightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.Parse(user1.Id), Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01], User = user1 },
            new() { Id = Guid.NewGuid(), UserId = Guid.Parse(user2.Id), Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ImageBuffer = [0x01], User = user2 }
        };

        _mockSightingsService
            .Setup(s => s.GetAllSightingsAsync())
            .ReturnsAsync(sightings);

        // Act
        var result = await _controller.Gallery() as ViewResult;
        var vm = result!.Model as SightingGalleryViewModel;

        // Assert: both sightings are present
        Assert.That(vm!.SightingCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Gallery_EachCardIncludesSubmitterUserId()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user1@test.com" };

        var sightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.Parse(user1.Id), Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01], User = user1 }
        };

        _mockSightingsService
            .Setup(s => s.GetAllSightingsAsync())
            .ReturnsAsync(sightings);

        // Act
        var result = await _controller.Gallery() as ViewResult;
        var vm = result!.Model as SightingGalleryViewModel;

        // Assert: the card carries the submitter's user ID for client-side filtering
        Assert.That(vm!.Sightings[0].SubmittedByUserId, Is.EqualTo(user1.Id));
    }

    #endregion
}
