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
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("accessed the sightings map")),
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
        // Arrange
        _mockSightingsService.Setup(s => s.GetSightingsInBoundsAsync(
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .ReturnsAsync(new List<Sighting>());

        // Act
        var result = await _controller.GetSightings(45.0, 46.0, -123.0, -122.0) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        var sightings = result.Value as IEnumerable<object>;
        Assert.That(sightings, Is.Not.Null);
        Assert.That(sightings.Count(), Is.EqualTo(0));
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetched") && v.ToString()!.Contains("sightings")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // [CSP-217] Timestamps in the JSON payload should be converted from UTC
    // to the timezone advertised by the UserTimeZone cookie. Falls back to PST.

    [Test]
    public async Task GetSightings_ConvertsTimestampToCookieTimezone()
    {
        // Arrange: a sighting recorded at 18:00 UTC, cookie says America/Los_Angeles (UTC-8 / -7 DST).
        var utcMoment = new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero);
        var sighting = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 45.5m,
            Longitude = -123.5m,
            Description = "Test",
            Timestamp = utcMoment,
            SpeciesName = "Test",
            Rarity = "Common",
            ImageBuffer = new byte[] { 0x01 }
        };
        _mockSightingsService.Setup(s => s.GetSightingsInBoundsAsync(
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .ReturnsAsync(new List<Sighting> { sighting });

        SetUserTimeZoneCookie("America/Los_Angeles");

        // Act
        var result = await _controller.GetSightings(45.0, 46.0, -124.0, -123.0) as JsonResult;

        // Assert
        var timestamp = GetFirstTimestamp(result!);
        var expected = TimeZoneInfo.ConvertTime(utcMoment,
            TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"))
            .ToString("MMM dd, yyyy h:mm tt");
        Assert.That(timestamp, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetSightings_FallsBackToPacificTime_WhenCookieMissing()
    {
        // Arrange: no cookie at all; controller should default to America/Los_Angeles.
        var utcMoment = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var sighting = BuildSighting(utcMoment);
        _mockSightingsService.Setup(s => s.GetSightingsInBoundsAsync(
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .ReturnsAsync(new List<Sighting> { sighting });

        // Act
        var result = await _controller.GetSightings(45.0, 46.0, -124.0, -123.0) as JsonResult;

        // Assert
        var timestamp = GetFirstTimestamp(result!);
        var expected = TimeZoneInfo.ConvertTime(utcMoment,
            TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"))
            .ToString("MMM dd, yyyy h:mm tt");
        Assert.That(timestamp, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetSightings_FallsBackToPacificTime_WhenCookieInvalid()
    {
        // Arrange: garbage cookie value -> FindSystemTimeZoneById throws -> PST fallback path runs.
        var utcMoment = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var sighting = BuildSighting(utcMoment);
        _mockSightingsService.Setup(s => s.GetSightingsInBoundsAsync(
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .ReturnsAsync(new List<Sighting> { sighting });

        SetUserTimeZoneCookie("Not/A_Real_Zone");

        // Act
        var result = await _controller.GetSightings(45.0, 46.0, -124.0, -123.0) as JsonResult;

        // Assert
        var timestamp = GetFirstTimestamp(result!);
        var expected = TimeZoneInfo.ConvertTime(utcMoment,
            TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"))
            .ToString("MMM dd, yyyy h:mm tt");
        Assert.That(timestamp, Is.EqualTo(expected));
    }

    private void SetUserTimeZoneCookie(string ianaId)
    {
        var ctx = (DefaultHttpContext)_controller.ControllerContext.HttpContext;
        ctx.Request.Headers["Cookie"] = $"UserTimeZone={ianaId}";
    }

    private static Sighting BuildSighting(DateTimeOffset timestamp) => new Sighting
    {
        Id = Guid.NewGuid(),
        Latitude = 45.5m,
        Longitude = -123.5m,
        Description = "Test",
        Timestamp = timestamp,
        SpeciesName = "Test",
        Rarity = "Common",
        ImageBuffer = new byte[] { 0x01 }
    };

    private static string GetFirstTimestamp(JsonResult result)
    {
        var items = (result.Value as System.Collections.IEnumerable)!;
        var first = items.Cast<object>().First();
        var prop = first.GetType().GetProperty("timestamp")!;
        return (string)prop.GetValue(first)!;
    }
}