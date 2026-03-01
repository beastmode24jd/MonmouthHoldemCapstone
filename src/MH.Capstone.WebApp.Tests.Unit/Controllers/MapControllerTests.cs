using System.Security.Claims;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
public class MapControllerTests
{
    private Mock<ILogger<MapController>> _mockLogger;
    private Mock<ISightingsService> _mockSightingsService;
    private Mock<UserManager<ApplicationUser>> _mockUserManager;
    private MapController _controller;
    private const string TestEmail = "testuser@example.com";

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<MapController>>();
        _mockSightingsService = new Mock<ISightingsService>();
        
        // Mock UserManager
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _controller = new MapController(
            _mockLogger.Object,
            _mockSightingsService.Object,
            _mockUserManager.Object);

        // Mock authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, TestEmail),
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public void Index_ReturnsViewResult()
    {
        // Act
        var result = _controller.Index();

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public void Index_LogsUserAccess()
    {
        // Act
        _controller.Index();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("accessed the sightings map")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetSightings_ReturnsJsonResult()
    {
        // Act
        var result = await _controller.GetSightings(null, null, null, null);

        // Assert
        Assert.That(result, Is.InstanceOf<JsonResult>());
    }

    [Test]
    public async Task GetSightings_ReturnsEmptyList_WhenNoSightingsExist()
    {
        // Act
        var result = await _controller.GetSightings(45.0, 46.0, -123.0, -122.0) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.InstanceOf<List<object>>());
        var sightings = result.Value as List<object>;
        Assert.That(sightings, Is.Empty);
    }

    [Test]
    public async Task GetSightings_WithBoundsParameters_LogsFetchedCount()
    {
        // Arrange
        double minLat = 45.0;
        double maxLat = 46.0;
        double minLng = -123.0;
        double maxLng = -122.0;

        // Act
        await _controller.GetSightings(minLat, maxLat, minLng, maxLng);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetched") && v.ToString().Contains("sightings")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}