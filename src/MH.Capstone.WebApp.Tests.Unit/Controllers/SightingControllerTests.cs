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
    private Mock<IPhotoQualityService> _mockPhotoQualityService = null!;
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
        _mockPhotoQualityService = new Mock<IPhotoQualityService>();

        // Default no-op result so existing tests that don't care about photo quality still run.
        _mockPhotoQualityService
            .Setup(p => p.AnalyzeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PhotoQualityTier.Unknown, 0.0, 0.0, 0, 0));

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new SightingController(
            _mockLogger.Object,
            _mockSightingsService.Object,
            _mockUserManager.Object,
            _mockBadgeService.Object,
            _mockPhotoQualityService.Object);

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

    #region CSP-122: Photo quality integration

    [Test]
    public async Task Upload_Post_WithValidImage_AnalyzesPhotoAndPersistsQualityMetadataOnSighting()
    {
        // Arrange — a real (small) IFormFile so ToByteArray inside ToDataModel returns non-empty.
        var imageBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var formFile = new FormFile(
            new MemoryStream(imageBytes), 0, imageBytes.Length, "UploadedImage", "owl.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var vm = new SightingUploadViewModel
        {
            Latitude = 44.85m,
            Longitude = -123.23m,
            Description = "Owl",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            UploadedImage = formFile,
            DeviceTimezone = "America/Los_Angeles"
        };

        _mockSightingsService
            .Setup(s => s.ValidateImage(It.IsAny<IFormFile>()))
            .Returns(true);

        // Capture the Sighting handed to CreateSightingAsync so we can inspect its quality fields.
        Sighting? captured = null;
        _mockSightingsService
            .Setup(s => s.CreateSightingAsync(It.IsAny<Sighting>(), It.IsAny<string>()))
            .Callback<Sighting, string>((s, _) => captured = s)
            .ReturnsAsync(10);

        var qualityResult = (
            Tier: PhotoQualityTier.High,
            Sharpness: 450.0,
            Luminance: 0.55,
            Width: 2400,
            Height: 1800);
        _mockPhotoQualityService
            .Setup(p => p.AnalyzeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(qualityResult);

        // Act
        await _controller.Upload(vm);

        // Assert — analyzer was invoked exactly once, and every metadata field flows onto the saved Sighting.
        _mockPhotoQualityService.Verify(
            p => p.AnalyzeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.That(captured, Is.Not.Null, "controller should have called CreateSightingAsync");
        Assert.That(captured!.QualityTier, Is.EqualTo(PhotoQualityTier.High));
        Assert.That(captured.SharpnessScore, Is.EqualTo(450.0));
        Assert.That(captured.LuminanceAverage, Is.EqualTo(0.55));
        Assert.That(captured.ResolutionWidth, Is.EqualTo(2400));
        Assert.That(captured.ResolutionHeight, Is.EqualTo(1800));
    }

    #endregion
}
