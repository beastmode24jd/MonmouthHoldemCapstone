using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
    private Mock<IAnimalFunFactService> _mockFunFactService = null!;
    private Mock<ICommentService> _mockCommentService = null!;
    private Mock<IUserService> _mockUserService = null!;
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
        _mockFunFactService = new Mock<IAnimalFunFactService>();
        _mockCommentService = new Mock<ICommentService>();
        _mockUserService = new Mock<IUserService>();

        _mockCommentService
            .Setup(c => c.GetCommentsForSightingAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Comment>());

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
            _mockPhotoQualityService.Object,
            _mockFunFactService.Object,
            _mockCommentService.Object,
            _mockUserService.Object);

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

        // TempData carries the photo-quality flash message across the redirect to Dashboard.
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());

        _mockUserManager
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(TestUser);
    }

    [TearDown]
    public void TearDown() => _controller?.Dispose();

    #region CSP-96: Gallery action tests

    // CSP-199: small helper so each test can wrap an in-memory list of sightings
    // into a PagedResult to feed the new paged service method. Defaults match
    // the controller's gallery page size (20).
    private static PagedResult<Sighting> AsPage(
        IList<Sighting> items, int page = 1, int pageSize = 20, int? totalCount = null)
        => new PagedResult<Sighting>(
            items.ToList(),
            totalCount ?? items.Count,
            page,
            pageSize);

    [Test]
    public async Task Gallery_ReturnsViewResult_WithSightingGalleryViewModel()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(AsPage(new List<Sighting>()));

        // Act
        var result = await _controller.Gallery() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.InstanceOf<SightingGalleryViewModel>());
    }

    [Test]
    public async Task Gallery_CallsGetSightingsPageAsync_NotLegacyLoadEverything()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(AsPage(new List<Sighting>()));

        // Act
        await _controller.Gallery();

        // Assert: CSP-199 — the controller must use the paged method. The
        // load-everything overloads from before pagination must NOT be invoked.
        _mockSightingsService.Verify(
            s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        _mockSightingsService.Verify(
            s => s.GetAllSightingsAsync(), Times.Never);
        _mockSightingsService.Verify(
            s => s.GetUserSightingsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Gallery_SetsCurrentUserIdOnViewModel()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(AsPage(new List<Sighting>()));

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
            .Setup(s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(AsPage(sightings));

        // Act
        var result = await _controller.Gallery() as ViewResult;
        var vm = result!.Model as SightingGalleryViewModel;

        // Assert: both sightings on this page are present
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
            .Setup(s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(AsPage(sightings));

        // Act
        var result = await _controller.Gallery() as ViewResult;
        var vm = result!.Model as SightingGalleryViewModel;

        // Assert: the card carries the submitter's user ID for client-side filtering
        Assert.That(vm!.Sightings[0].SubmittedByUserId, Is.EqualTo(user1.Id));
    }

    #endregion

    #region CSP-199: Gallery pagination behavior

    // Pins: visiting /Sighting/Gallery with no query string defaults to page 1 and
    // requests the gallery's configured page size (20, within the ticket's 10-20 range).
    [Test]
    public async Task Gallery_NoPageParam_RequestsFirstPageWithGallerySize()
    {
        _mockSightingsService
            .Setup(s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(AsPage(new List<Sighting>()));

        await _controller.Gallery();

        _mockSightingsService.Verify(
            s => s.GetSightingsPageAsync(1, 20), Times.Once);
    }

    // Pins: /Sighting/Gallery?page=3 actually forwards page 3 to the service.
    [Test]
    public async Task Gallery_WithPageParam_PassesPageToService()
    {
        _mockSightingsService
            .Setup(s => s.GetSightingsPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(AsPage(new List<Sighting>(), page: 3));

        await _controller.Gallery(page: 3);

        _mockSightingsService.Verify(
            s => s.GetSightingsPageAsync(3, 20), Times.Once);
    }

    // Pins: paging metadata (CurrentPage / TotalPages / TotalCount / HasPrev / HasNext)
    // lands on the ViewModel so the view can render the Prev/Next controls correctly.
    [Test]
    public async Task Gallery_SurfacesPagingMetadataOnViewModel()
    {
        // 45 total sightings, size 20 -> 3 pages. Viewing page 2 means both Prev and Next exist.
        var sightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(),
                    Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01] }
        };
        _mockSightingsService
            .Setup(s => s.GetSightingsPageAsync(2, 20))
            .ReturnsAsync(AsPage(sightings, page: 2, pageSize: 20, totalCount: 45));

        var result = await _controller.Gallery(page: 2) as ViewResult;
        var vm = result!.Model as SightingGalleryViewModel;

        Assert.That(vm!.CurrentPage, Is.EqualTo(2));
        Assert.That(vm.TotalCount, Is.EqualTo(45));
        Assert.That(vm.TotalPages, Is.EqualTo(3));
        Assert.That(vm.HasPreviousPage, Is.True);
        Assert.That(vm.HasNextPage, Is.True);
    }

    #endregion

    #region CSP-172: Sighting Details action tests

    private static Sighting BuildSightingForDetails(Guid? id = null, string speciesName = "Coyote")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "alex@test.com",
            Email = "alex@test.com",
            DisplayName = "Alex"
        };

        return new Sighting
        {
            Id = id ?? Guid.NewGuid(),
            UserIdentityId = user.Id,
            Latitude = 44.85m,
            Longitude = -123.23m,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-2),
            Description = "A coyote in a field at dusk.",
            ImageBuffer = [0x01, 0x02, 0x03],
            SpeciesName = speciesName,
            Rarity = "Common",
            RarityMultiplier = 1.0,
            User = user
        };
    }

    [Test]
    public async Task Details_ValidId_ReturnsViewWithPopulatedSightingDetailsViewModel()
    {
        // Arrange
        var sightingId = Guid.NewGuid();
        var sighting = BuildSightingForDetails(sightingId, speciesName: "Coyote");

        _mockSightingsService
            .Setup(s => s.GetSightingByIdAsync(It.Is<Guid>(g => g == sightingId)))
            .ReturnsAsync(sighting)
            .Verifiable(Times.Once);

        _mockFunFactService
            .Setup(f => f.GetFunFactAsync(It.Is<string>(n => n == "Coyote"), It.IsAny<CancellationToken>()))
            .ReturnsAsync("The trickster of the American West!")
            .Verifiable(Times.Once);

        // Act
        var result = await _controller.Details(sightingId) as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null, "Details action should return a ViewResult");
        Assert.That(result!.Model, Is.InstanceOf<SightingDetailsViewModel>());

        var vm = (SightingDetailsViewModel)result.Model!;
        Assert.That(vm.Id, Is.EqualTo(sightingId));
        Assert.That(vm.SpeciesName, Is.EqualTo("Coyote"));
        Assert.That(vm.UploaderDisplayName, Is.EqualTo("Alex"));
        Assert.That(vm.Description, Is.EqualTo("A coyote in a field at dusk."));
        Assert.That(vm.FunFact, Is.EqualTo("The trickster of the American West!"));
        Assert.That(vm.IsFunFactFallback, Is.False);

        _mockSightingsService.Verify();
        _mockFunFactService.Verify();
    }

    [Test]
    public async Task Details_UnknownId_ReturnsNotFoundViewAndDoesNotCallFunFactService()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        _mockSightingsService
            .Setup(s => s.GetSightingByIdAsync(It.Is<Guid>(g => g == unknownId)))
            .ReturnsAsync((Sighting?)null)
            .Verifiable(Times.Once);

        // Act
        var result = await _controller.Details(unknownId) as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null,
            "missing sightings should still return a styled view, not a 404 status");
        Assert.That(result!.ViewName, Is.EqualTo("NotFound"),
            "the controller should render the styled NotFound view for unknown IDs");

        _mockSightingsService.Verify();
        _mockFunFactService.Verify(
            f => f.GetFunFactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "fun-fact lookup should not run when there is no sighting to render");
    }

    [Test]
    public async Task Details_FunFactReturnsNull_ViewModelMarkedAsFallbackWithFriendlyText()
    {
        // Arrange
        var sightingId = Guid.NewGuid();
        var sighting = BuildSightingForDetails(sightingId, speciesName: "Mystery Critter Z");

        _mockSightingsService
            .Setup(s => s.GetSightingByIdAsync(It.Is<Guid>(g => g == sightingId)))
            .ReturnsAsync(sighting);

        _mockFunFactService
            .Setup(f => f.GetFunFactAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.Details(sightingId) as ViewResult;
        var vm = result!.Model as SightingDetailsViewModel;

        // Assert
        Assert.That(vm, Is.Not.Null);
        Assert.That(vm!.IsFunFactFallback, Is.True,
            "when the fun-fact lookup returns null, the VM must signal fallback for the view");
        Assert.That(vm.FunFact, Is.EqualTo(SightingDetailsViewModel.FunFactFallbackMessage),
            "the friendly fallback text must be the canonical message defined on the view model");
    }

    #endregion

    #region CSP-37: Edit action tests

    // A sighting owned by the logged-in TestUser (so ownership checks pass).
    private static Sighting BuildOwnedSighting(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserIdentityId = TestUserId,
        Latitude = 44.5m,
        Longitude = -123.25m,
        Timestamp = DateTimeOffset.UtcNow.AddDays(-2),
        Description = "Original description",
        SpeciesName = "Coyote",
        ImageBuffer = [0x01, 0x02, 0x03]
    };

    // A sighting owned by somebody else.
    private static Sighting BuildOthersSighting(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserIdentityId = Guid.NewGuid().ToString(),
        Latitude = 10m,
        Longitude = 10m,
        Timestamp = DateTimeOffset.UtcNow.AddDays(-2),
        Description = "Someone else's sighting",
        SpeciesName = "Bald Eagle",
        ImageBuffer = [0x09]
    };

    [Test]
    public async Task EditGet_OwnerExistingSighting_ReturnsViewWithPrePopulatedEditViewModel()
    {
        var id = Guid.NewGuid();
        var sighting = BuildOwnedSighting(id);
        _mockSightingsService.Setup(s => s.GetSightingByIdAsync(id)).ReturnsAsync(sighting);

        var result = await _controller.Edit(id) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.InstanceOf<SightingEditViewModel>());
        var vm = (SightingEditViewModel)result.Model!;
        Assert.That(vm.Id, Is.EqualTo(id));
        Assert.That(vm.Description, Is.EqualTo("Original description"));
        Assert.That(vm.SpeciesName, Is.EqualTo("Coyote"));
    }

    [Test]
    public async Task EditGet_NonOwner_ReturnsForbid()
    {
        var id = Guid.NewGuid();
        _mockSightingsService.Setup(s => s.GetSightingByIdAsync(id)).ReturnsAsync(BuildOthersSighting(id));

        // 403 → cookie auth redirects to the configured AccessDeniedPath at runtime.
        var result = await _controller.Edit(id);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task EditGet_UnknownId_ReturnsNotFoundView()
    {
        var id = Guid.NewGuid();
        _mockSightingsService.Setup(s => s.GetSightingByIdAsync(id)).ReturnsAsync((Sighting?)null);

        var result = await _controller.Edit(id) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ViewName, Is.EqualTo("NotFound"));
    }

    [Test]
    public async Task EditPost_OwnerValidModel_UpdatesAndRedirectsToDetailsWithFlash()
    {
        var id = Guid.NewGuid();
        var sighting = BuildOwnedSighting(id);
        _mockSightingsService.Setup(s => s.GetSightingByIdAsync(id)).ReturnsAsync(sighting);
        _mockSightingsService
            .Setup(s => s.UpdateSightingAsync(id, It.IsAny<Guid>(), "Updated", "Gray Wolf"))
            .ReturnsAsync(sighting)
            .Verifiable(Times.Once);

        var model = new SightingEditViewModel { Id = id, Description = "Updated", SpeciesName = "Gray Wolf" };

        var result = await _controller.Edit(id, model) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ActionName, Is.EqualTo("Details"));
        Assert.That(_controller.TempData["EditSuccess"], Is.Not.Null);
        _mockSightingsService.Verify();
    }

    [Test]
    public async Task EditPost_EmptyDescription_ReturnsViewAndDoesNotUpdate()
    {
        var id = Guid.NewGuid();
        _mockSightingsService.Setup(s => s.GetSightingByIdAsync(id)).ReturnsAsync(BuildOwnedSighting(id));
        _controller.ModelState.AddModelError(nameof(SightingEditViewModel.Description), "Required");

        var model = new SightingEditViewModel { Id = id, Description = "", SpeciesName = "Coyote" };

        var result = await _controller.Edit(id, model) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Model, Is.InstanceOf<SightingEditViewModel>());
        _mockSightingsService.Verify(
            s => s.UpdateSightingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task EditPost_EmptySpeciesName_ReturnsViewAndDoesNotUpdate()
    {
        var id = Guid.NewGuid();
        _mockSightingsService.Setup(s => s.GetSightingByIdAsync(id)).ReturnsAsync(BuildOwnedSighting(id));
        _controller.ModelState.AddModelError(nameof(SightingEditViewModel.SpeciesName), "Required");

        var model = new SightingEditViewModel { Id = id, Description = "Updated", SpeciesName = "" };

        var result = await _controller.Edit(id, model) as ViewResult;

        Assert.That(result, Is.Not.Null);
        _mockSightingsService.Verify(
            s => s.UpdateSightingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task EditPost_NonOwner_ReturnsForbidAndDoesNotUpdate()
    {
        var id = Guid.NewGuid();
        _mockSightingsService.Setup(s => s.GetSightingByIdAsync(id)).ReturnsAsync(BuildOthersSighting(id));

        var model = new SightingEditViewModel { Id = id, Description = "hijacked", SpeciesName = "Fake" };

        var result = await _controller.Edit(id, model);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
        _mockSightingsService.Verify(
            s => s.UpdateSightingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region CSP-122: Photo quality integration

    // Builds a SightingUploadViewModel with a small but non-empty IFormFile
    // attached, so that ToDataModel produces a non-empty ImageBuffer and the
    // controller has something to hand to the analyzer.
    private static SightingUploadViewModel BuildViewModelWithImage()
    {
        var imageBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var formFile = new FormFile(
            new MemoryStream(imageBytes), 0, imageBytes.Length, "UploadedImage", "owl.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
        return new SightingUploadViewModel
        {
            Latitude = 44.85m,
            Longitude = -123.23m,
            Description = "Owl",
            SpeciesName = "Great Horned Owl",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            UploadedImage = formFile,
            DeviceTimezone = "America/Los_Angeles"
        };
    }

    [Test]
    public async Task Upload_Post_WithValidImage_AnalyzesPhotoAndPersistsQualityMetadataOnSighting()
    {
        // Arrange
        var vm = BuildViewModelWithImage();

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

    [Test]
    public async Task Upload_Post_WithLongSideBelow1024_RejectsSubmissionWithModelError()
    {
        // Arrange — image whose long side is below the resolution gate (800 < 1024).
        var vm = BuildViewModelWithImage();

        _mockSightingsService
            .Setup(s => s.ValidateImage(It.IsAny<IFormFile>()))
            .Returns(true);

        _mockPhotoQualityService
            .Setup(p => p.AnalyzeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PhotoQualityTier.Low, 50.0, 0.5, 800, 600));

        // Act
        var result = await _controller.Upload(vm);

        // Assert — controller returns the Upload view with a model error and no sighting is saved.
        Assert.That(result, Is.InstanceOf<ViewResult>(),
            "submission should be rejected back to the Upload view, not redirected");
        Assert.That(_controller.ModelState.IsValid, Is.False);

        var imageErrors = _controller.ModelState[nameof(SightingUploadViewModel.UploadedImage)]?.Errors;
        Assert.That(imageErrors, Is.Not.Null.And.Not.Empty);
        Assert.That(
            imageErrors!.Any(e => e.ErrorMessage.Contains("higher-resolution original")),
            Is.True,
            "rejection message must reference 'higher-resolution original' per CSP-122 spec");

        _mockSightingsService.Verify(
            s => s.CreateSightingAsync(It.IsAny<Sighting>(), It.IsAny<string>()),
            Times.Never,
            "no sighting should be persisted when the resolution gate rejects the image");
    }

    [TestCase(PhotoQualityTier.High,   450.0, 0.55)]
    [TestCase(PhotoQualityTier.Medium, 200.0, 0.50)]
    public async Task Upload_Post_AcceptedTier_SetsSingleSuccessFlash(
        PhotoQualityTier tier, double sharpness, double luminance)
    {
        // CSP-189: High and Medium tiers both succeed with the same confirmation flash.
        var vm = BuildViewModelWithImage();

        _mockSightingsService
            .Setup(s => s.ValidateImage(It.IsAny<IFormFile>()))
            .Returns(true);
        _mockSightingsService
            .Setup(s => s.CreateSightingAsync(It.IsAny<Sighting>(), It.IsAny<string>()))
            .ReturnsAsync(10);
        _mockPhotoQualityService
            .Setup(p => p.AnalyzeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((tier, sharpness, luminance, 2400, 1800));

        await _controller.Upload(vm);

        var success = _controller.TempData["PhotoQualitySuccess"] as string;
        Assert.That(success, Is.Not.Null, "accepted uploads should set the success flash");
        Assert.That(success, Does.Contain("Great photo").IgnoreCase);
        Assert.That(_controller.TempData["PhotoQualityWarning"], Is.Null,
            "warning flash is no longer used — Low tier rejects at the form instead");
    }

    [TestCase(50.0,  0.50, "blurry")]
    [TestCase(0.0,   0.10, "too dark")]
    [TestCase(0.0,   0.95, "overexposed")]
    public async Task Upload_Post_WithLowTier_RejectsSubmissionWithReasonMessage(
        double sharpness, double luminance, string expectedReasonSubstring)
    {
        // CSP-189: Low tier no longer saves with a warning — it returns to the form
        // with a ModelState error explaining why so the user can re-upload.
        var vm = BuildViewModelWithImage();

        _mockSightingsService
            .Setup(s => s.ValidateImage(It.IsAny<IFormFile>()))
            .Returns(true);
        _mockPhotoQualityService
            .Setup(p => p.AnalyzeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PhotoQualityTier.Low, sharpness, luminance, 2400, 1800));
        _mockPhotoQualityService
            .Setup(p => p.GetLowQualityReasonMessage(sharpness, luminance))
            .Returns($"This photo is {expectedReasonSubstring}. Try again.");

        var result = await _controller.Upload(vm);

        Assert.That(result, Is.InstanceOf<ViewResult>(),
            "Low-tier submissions should return the Upload view, not redirect");
        Assert.That(_controller.ModelState.IsValid, Is.False);

        var imageErrors = _controller.ModelState[nameof(SightingUploadViewModel.UploadedImage)]?.Errors;
        Assert.That(imageErrors, Is.Not.Null.And.Not.Empty);
        Assert.That(
            imageErrors!.Any(e => e.ErrorMessage.Contains(expectedReasonSubstring,
                StringComparison.OrdinalIgnoreCase)),
            Is.True,
            $"ModelState error should mention the low-quality reason '{expectedReasonSubstring}'");

        _mockSightingsService.Verify(
            s => s.CreateSightingAsync(It.IsAny<Sighting>(), It.IsAny<string>()),
            Times.Never,
            "Low-tier photo must not be persisted");
    }

    [Test]
    public async Task Upload_Post_WithLongSideExactly1024_PassesGateAndCreatesSighting()
    {
        // Arrange — boundary case: long side equals the threshold, so the image passes.
        var vm = BuildViewModelWithImage();

        _mockSightingsService
            .Setup(s => s.ValidateImage(It.IsAny<IFormFile>()))
            .Returns(true);
        _mockSightingsService
            .Setup(s => s.CreateSightingAsync(It.IsAny<Sighting>(), It.IsAny<string>()))
            .ReturnsAsync(10);

        _mockPhotoQualityService
            .Setup(p => p.AnalyzeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PhotoQualityTier.Medium, 200.0, 0.5, 1024, 768));

        // Act
        await _controller.Upload(vm);

        // Assert — gate allowed it through; sighting was persisted exactly once.
        _mockSightingsService.Verify(
            s => s.CreateSightingAsync(It.IsAny<Sighting>(), It.IsAny<string>()),
            Times.Once);
        Assert.That(_controller.ModelState.IsValid, Is.True);
    }

    #endregion
}
