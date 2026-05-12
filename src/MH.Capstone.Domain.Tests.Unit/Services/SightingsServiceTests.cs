using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using static MH.Capstone.Tests.SharedInternals.RandomData;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Internal;
using MH.Capstone.Domain.DataAccess;
using System.Linq.Expressions;

#pragma warning disable CA1416

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class SightingsServiceTests
{
    private Sighting _validSighting;
    private Mock<IScoringService> _scoringServiceMock;
    private Mock<INotificationService> _notificationServiceMock;
    private Mock<IRepository<Sighting, ApplicationDbContext>> _sightingsRepoMock;
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;
    private Mock<IBadgeService> _badgeServiceMock;
    private FakeImageGenerator _imageGenerator;

    // Remember: Arrange, Act, Assert
    [SetUp]
    public void Setup()
    {
        _imageGenerator = new FakeImageGenerator();
        _validSighting = SightingValidValuesSource.DefaultValidSighting;
        _sightingsRepoMock = new Mock<IRepository<Sighting, ApplicationDbContext>>();
        _scoringServiceMock = new Mock<IScoringService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();
        _badgeServiceMock = new Mock<IBadgeService>();

        // GLOBAL MOCKS for new dependencies
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync(It.IsAny<string>()))
            .ReturnsAsync(10);

        // Mock the metadata tuple return
        _scoringServiceMock.Setup(s => s.GetRarityMultiplierAndName(It.IsAny<int>()))
            .ReturnsAsync((1.0, "Common"));

        // Provide an empty list of users by default so FirstOrDefault doesn't crash
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser>().AsQueryable());

        // Default: return empty sightings for any predicate query (covers unique species count after upload)
        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Sighting>().AsQueryable());
    }

    [TearDown]
    public void TearDown()
    {
        _imageGenerator.Dispose();
    }

    private SightingsService CreateSut() =>
        new(NullLogger<SightingsService>.Instance,
            _scoringServiceMock.Object,
            _notificationServiceMock.Object,
            _sightingsRepoMock.Object,
            _userRepoMock.Object,
            _badgeServiceMock.Object);

    private void AssertAllMockVerifications()
    {
        // Asserts that the methods that were set up in the Moq were called in ways that we set up
        // Global Setup calls act as defaults
        _sightingsRepoMock.Verify();
        _scoringServiceMock.Verify();
        _notificationServiceMock.Verify();
        _userRepoMock.Verify();
        _badgeServiceMock.Verify();
    }

    // Will run 2^4 = 16 times, testing all combinations of the valid values for lat, long, timestamp, and description
    [Test]
    public void CreateSightingAsync_ValidSighting_ReturnsSuccessfulTaskWithoutException(
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidLats))] decimal lat,
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidLongs))] decimal lon,
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidTimestamps))] DateTimeOffset timestamp,
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidDescriptions))] string desc)
    {
        // Arrange
        var sighting = new Sighting(_validSighting.Id, _validSighting.UserId, lat, lon,
            timestamp, desc, [0x01], 10, false, "Common", 1.0);
        var sightingsCount = GetRandomIntInRange(1, 100);
        var pointsValue = GetRandomIntInRange(1, 20);

        _sightingsRepoMock.Setup(r =>
            r.AddOrUpdateAsync(It.Is(sighting, SightingComparer.Instance)))
            .ReturnsAsync(sighting).Verifiable(Times.Once);

        // CSP-142: scoring lookup keys off SpeciesName instead of placeholder int id.
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync(
            It.Is<string>(n => n == sighting.SpeciesName)))
            .ReturnsAsync(sightingsCount).Verifiable(Times.Once);

        _scoringServiceMock.Setup(s => s.CalculatePointsAsync(
            It.Is<int>(i => i == sightingsCount)))
            .ReturnsAsync(pointsValue).Verifiable(Times.Once);

        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(value: new List<ApplicationUser> { new ApplicationUser { Id = sighting.UserIdentityId } }.AsQueryable())
            .Verifiable(Times.Once);

        _userRepoMock.Setup(r => r.AddOrUpdateAsync(
            It.Is<ApplicationUser>(u => u.Id == sighting.UserIdentityId &&
                u.Points == pointsValue)))
            .Verifiable(Times.Once);

        _notificationServiceMock.Setup(s => s.SendNotificationAsync(
            It.Is<Notification>(n => n.RecipientId == sighting.UserId), It.IsAny<NotificationType>()))
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act & Assert
        Assert.DoesNotThrowAsync(() => sut.CreateSightingAsync(sighting));
        AssertAllMockVerifications();
    }

    [Test]
    public void CreateSightingAsync_ValidImage_ReturnsSuccessfulTaskWithoutException()
    {
        // Arrange
        var sighting = _validSighting;
        sighting.ImageBuffer = _imageGenerator.GetValidImage().ToByteArray();
        var sightingsCount = GetRandomIntInRange(1, 100);
        var pointsValue = GetRandomIntInRange(1, 20);

        _sightingsRepoMock.Setup(r =>
                r.AddOrUpdateAsync(It.Is(sighting, SightingComparer.Instance)))
            .ReturnsAsync(sighting).Verifiable(Times.Once);

        // CSP-142: scoring lookup keys off SpeciesName instead of placeholder int id.
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync(
                It.Is<string>(n => n == sighting.SpeciesName)))
            .ReturnsAsync(sightingsCount).Verifiable(Times.Once);

        _scoringServiceMock.Setup(s => s.CalculatePointsAsync(
                It.Is<int>(i => i == sightingsCount)))
            .ReturnsAsync(pointsValue).Verifiable(Times.Once);

        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(value: new List<ApplicationUser> { new ApplicationUser { Id = sighting.UserIdentityId } }.AsQueryable())
            .Verifiable(Times.Once);

        _userRepoMock.Setup(r => r.AddOrUpdateAsync(
                It.Is<ApplicationUser>(u => u.Id == sighting.UserIdentityId &&
                                            u.Points == pointsValue)))
            .Verifiable(Times.Once);

        _notificationServiceMock.Setup(s => s.SendNotificationAsync(
                It.Is<Notification>(n => n.RecipientId == sighting.UserId), It.IsAny<NotificationType>()))
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Fix 1: Ensure the ImageBuffer isn't empty (use a mock PNG header)
        sighting.ImageBuffer = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Fix 2: Pass the required TimeZone parameter
        Assert.DoesNotThrowAsync(() => sut.CreateSightingAsync(sighting, "America/Los_Angeles"));
        AssertAllMockVerifications();
    }

    // Lat can range from -90 to 90 inclusive, so we test just outside those bounds
    [TestCase(-91)]
    [TestCase(99)]
    public void CreateSightingAsync_InvalidLatitude_ReturnsFailedTaskThrowingArgumentException(decimal lat)
    {
        // Arrange
        var sighting = _validSighting;
        sighting.Latitude = lat;

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSightingAsync(sighting));
        AssertAllMockVerifications();
    }

    // Long can range from -180 to 180 inclusive, so we test just outside those bounds
    [TestCase(-192)]
    [TestCase(199)]
    public void CreateSightingAsync_InvalidLongitude_ReturnsFailedTaskThrowingArgumentException(decimal lon)
    {
        // Arrange
        var sighting = _validSighting;
        sighting.Longitude = lon;

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSightingAsync(sighting));
        AssertAllMockVerifications();
    }

    [Test]
    public void CreateSightingAsync_DateInFuture_ReturnsFailedTaskThrowingArgumentException()
    {
        // Arrange
        var sighting = _validSighting;
        sighting.Timestamp = DateTimeOffset.Now.AddHours(2);

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSightingAsync(sighting));
        AssertAllMockVerifications();
    }

    [Test]
    public void CreateSightingAsync_DescriptionTooLong_ReturnsFailedTaskThrowingArgumentException()
    {
        // Arrange
        var sighting = _validSighting;
        sighting.Description = GetRandomStringOfLength(GetRandomIntInRange(501, 600));

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSightingAsync(sighting));
        AssertAllMockVerifications();
    }

    [Test]
    public void CreateSightingAsync_UserDoesNotExist_ReturnsFailedTaskThrowingArgumentException()
    {
        // Arrange
        var sighting = _validSighting;
        _validSighting.UserId = Guid.AllBitsSet;

        // User cannot be null, to hit try block where exception is
        var user = new ApplicationUser { Id = sighting.UserIdentityId };

        // User exists in memory but database throws FK constraint on save
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser> { user }.AsQueryable());

        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()))
            .ThrowsAsync(new DbUpdateException("Foreign key violation", 
            new SqlExceptionBuilder().WithNumber(547).Build()))
            .Verifiable();

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSightingAsync(sighting));
        AssertAllMockVerifications();
    }

    [Test]
    public void CreateSightingAsync_InvalidImageFile_ReturnsFailedTaskThrowingArgumentException()
    {
        // Arrange
        var sighting = _validSighting;
        _validSighting.ImageBuffer = [];

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => sut.CreateSightingAsync(sighting));
        AssertAllMockVerifications();
    }

    [Test]
    public void ValidateImageAsync_NullImageFile_ReturnsFalse()
    {
        // Arrange
        IFormFile? imgFile = null;
        var sut = CreateSut();

        // Act & Assert
        Assert.That(sut.ValidateImage(imgFile), Is.False);
        AssertAllMockVerifications();
    }

    [Test]
    public void ValidateImageAsync_EmptyImageFile_ReturnsFalse()
    {
        // Arrange
        var imgFile = GenerateBadFormFile(Stream.Null, 0, 0, "empty_img_file.png");
        var sut = CreateSut();

        // Act & Assert
        Assert.That(sut.ValidateImage(imgFile), Is.False);
        AssertAllMockVerifications();
    }

    [Test]
    public async Task GetSightingsInBoundsAsync_ReturnsSightingsWithinBounds()
    {
        // Arrange
        var sightingInBounds = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 45.0m,
            Longitude = -123.0m,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            Description = "Test sighting",
            ImageBuffer = new byte[] { 0x01 }
        };

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting,bool>>>()))
            .ReturnsAsync(new List<Sighting> { sightingInBounds }.AsQueryable());

        var sut = CreateSut();

        // Act
        var result = await sut.GetSightingsInBoundsAsync(44.0m, 46.0m, -124.0m, -122.0m);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Id, Is.EqualTo(sightingInBounds.Id));
    }

    [Test]
    public async Task GetSightingsInBoundsAsync_ExcludesSightingsOutsideBounds()
    {
        // Arrange
        var sightingOutOfBounds = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 34.0m,  // Los Angeles area - outside Oregon bounds
            Longitude = -118.0m,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            Description = "Out of bounds sighting",
            ImageBuffer = new byte[] { 0x01 }
        };

        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sighting> { sightingOutOfBounds }.AsQueryable());

        var sut = CreateSut();

        // Act
        var result = await sut.GetSightingsInBoundsAsync(44.0m, 46.0m, -124.0m, -122.0m);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetSightingsInBoundsAsync_ExcludesSightingsOlderThanSevenDays()
    {
        // Arrange
        var oldSighting = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 45.0m,
            Longitude = -123.0m,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-10),  // 10 days old
            Description = "Old sighting",
            ImageBuffer = new byte[] { 0x01 }
        };

        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sighting> { oldSighting }.AsQueryable());

        var sut = CreateSut();

        // Act
        var result = await sut.GetSightingsInBoundsAsync(44.0m, 46.0m, -124.0m, -122.0m);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(0));
    }


    [TestCase("txt")]
    [TestCase("pdf")]
    [TestCase("cs")]
    [TestCase("bat")]
    [TestCase("file")]
    [TestCase("helic")]
    [TestCase("md")]
    [TestCase("sh")]
    [TestCase("pop")]
    [TestCase("mp3")]
    [TestCase("m4a")]
    [TestCase("mov")]
    [TestCase("wav")]
    [TestCase("")]
    public void ValidateImageAsync_NonImageFileExt_ReturnsFalse(string ext)
    {
        // Arrange
        var imgFile = GenerateBadFormFile(Stream.Null, 0, 0, $"empty_img_file.{ext}");
        var sut = CreateSut();

        // Act & Assert
        Assert.That(sut.ValidateImage(imgFile), Is.False);
        AssertAllMockVerifications();
    }

    [Test]
    public void ValidateImageAsync_TooLargeImageFile_ReturnsFalse()
    {
        // Arrange
        // The max allowed image size is 2 MB, so we test just above that limit. We use Stream.Null
        // since the ValidateImageAsync method should check the file size before attempting to read the stream,
        // so it should not throw an exception for the stream being unreadable.
        var imgSize = GetRandomIntInRange(2 * 1024 * 1024 + 1, 3 * 1024 * 1024);
        var imgFile = GenerateBadFormFile(Stream.Null, 0, imgSize, "empty_img_file.png");
        var sut = CreateSut();

        // Act & Assert
        Assert.That(sut.ValidateImage(imgFile), Is.False);
        AssertAllMockVerifications();
    }

    #region CSP-145: GetUserSightingsAsync Tests

    [Test]
    public async Task GetUserSightingsAsync_ValidUserId_ReturnsSightingsForThatUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTime.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTime.UtcNow.AddDays(-2), ImageBuffer = [0x01] }
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        var sut = CreateSut();

        // Act
        var result = await sut.GetUserSightingsAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        _sightingsRepoMock.Verify(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sighting, bool>>>()), Times.Once);
    }

    [Test]
    public async Task GetUserSightingsAsync_UserWithNoSightings_ReturnsEmptyEnumerable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptySightings = Enumerable.Empty<Sighting>().AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(emptySightings);

        var sut = CreateSut();

        // Act
        var result = await sut.GetUserSightingsAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetUserSightingsAsync_MultipleSightings_ReturnsOrderedByTimestampDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTime.UtcNow.AddDays(-5), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTime.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTime.UtcNow.AddDays(-3), ImageBuffer = [0x01] }
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        var sut = CreateSut();

        // Act
        var result = (await sut.GetUserSightingsAsync(userId)).ToList();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3));
        // Verify descending order: newest first
        Assert.That(result[0].Timestamp, Is.GreaterThan(result[1].Timestamp));
        Assert.That(result[1].Timestamp, Is.GreaterThan(result[2].Timestamp));
    }

    #endregion

    #region CSP-96: GetAllSightingsAsync Tests

    [Test]
    public async Task GetAllSightingsAsync_ReturnsSightingsFromAllUsersOrderedByTimestampDescending()
    {
        // Arrange
        var sightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow.AddDays(-3), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ImageBuffer = [0x01] }
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(sightings);

        var sut = CreateSut();

        // Act
        var result = (await sut.GetAllSightingsAsync()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result[0].Timestamp, Is.GreaterThan(result[1].Timestamp));
        Assert.That(result[1].Timestamp, Is.GreaterThan(result[2].Timestamp));
    }

    [Test]
    public async Task GetAllSightingsAsync_NoSightings_ReturnsEmpty()
    {
        // Arrange
        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, object>>[]>()))
            .ReturnsAsync(Enumerable.Empty<Sighting>().AsQueryable());

        var sut = CreateSut();

        // Act
        var result = await sut.GetAllSightingsAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region CSP-142: GetUserAnidexAsync Tests

    [Test]
    public async Task GetUserAnidexAsync_UserHasNoSightings_ReturnsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Sighting>().AsQueryable());

        var sut = CreateSut();

        // Act
        var result = await sut.GetUserAnidexAsync(userId);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetUserAnidexAsync_UserHasUniqueSpecies_ReturnsOneEntryPerSpecies()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote",     Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Bald Eagle", Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ImageBuffer = [0x02] },
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        var sut = CreateSut();

        // Act
        var result = (await sut.GetUserAnidexAsync(userId)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Select(e => e.SpeciesName), Is.EquivalentTo(new[] { "Coyote", "Bald Eagle" }));
    }

    [Test]
    public async Task GetUserAnidexAsync_UserSawSameSpeciesMultipleTimes_DiscoveryCountReflectsRepeats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ImageBuffer = [0x02] },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-3), ImageBuffer = [0x03] },
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        var sut = CreateSut();

        // Act
        var result = (await sut.GetUserAnidexAsync(userId)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SpeciesName, Is.EqualTo("Coyote"));
        Assert.That(result[0].DiscoveryCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetUserAnidexAsync_SpeciesNameCasingDiffers_GroupsAsSingleEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ImageBuffer = [0x02] },
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        var sut = CreateSut();

        // Act
        var result = (await sut.GetUserAnidexAsync(userId)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].DiscoveryCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetUserAnidexAsync_LatestImageBuffer_IsFromMostRecentSighting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var olderImage = new byte[] { 0xAA };
        var newerImage = new byte[] { 0xBB };

        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-5), ImageBuffer = olderImage },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = newerImage },
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        var sut = CreateSut();

        // Act
        var result = (await sut.GetUserAnidexAsync(userId)).Single();

        // Assert
        Assert.That(result.LatestImageBuffer, Is.EqualTo(newerImage));
    }

    [Test]
    public async Task GetUserAnidexAsync_RarityDerivedFromGlobalCountNotPerUserCount()
    {
        // Arrange — user has 2 Coyote sightings, but globally Coyote count is 100 (Common).
        var userId = Guid.NewGuid();
        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote", Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ImageBuffer = [0x02] },
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync("Coyote"))
            .ReturnsAsync(100);
        _scoringServiceMock.Setup(s => s.GetRarityMultiplierAndName(100))
            .ReturnsAsync((1.0, "Common"));

        var sut = CreateSut();

        // Act
        var result = (await sut.GetUserAnidexAsync(userId)).Single();

        // Assert — per-user count was 2 (would be Mythic if applied locally) but
        // global count is what determines rarity, so the entry must be Common.
        Assert.That(result.RarityName, Is.EqualTo("Common"));
        Assert.That(result.RarityMultiplier, Is.EqualTo(1.0));
        Assert.That(result.DiscoveryCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetUserAnidexAsync_ResultsOrderedByRarityDescendingThenAlphabetical()
    {
        // Arrange — user has three different species; mock distinct global counts to
        // exercise tier ordering (Mythic > Rare > Common).
        var userId = Guid.NewGuid();
        var userSightings = new List<Sighting>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Bobcat",     Timestamp = DateTimeOffset.UtcNow.AddDays(-1), ImageBuffer = [0x01] },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Coyote",     Timestamp = DateTimeOffset.UtcNow.AddDays(-2), ImageBuffer = [0x02] },
            new() { Id = Guid.NewGuid(), UserId = userId, SpeciesName = "Bald Eagle", Timestamp = DateTimeOffset.UtcNow.AddDays(-3), ImageBuffer = [0x03] },
        }.AsQueryable();

        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(userSightings);

        // Coyote=Common, Bobcat=Rare, Bald Eagle=Mythic
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync("Coyote")).ReturnsAsync(100);
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync("Bobcat")).ReturnsAsync(25);
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync("Bald Eagle")).ReturnsAsync(2);

        _scoringServiceMock.Setup(s => s.GetRarityMultiplierAndName(100)).ReturnsAsync((1.0, "Common"));
        _scoringServiceMock.Setup(s => s.GetRarityMultiplierAndName(25)).ReturnsAsync((2.0, "Rare"));
        _scoringServiceMock.Setup(s => s.GetRarityMultiplierAndName(2)).ReturnsAsync((5.0, "Mythic"));

        var sut = CreateSut();

        // Act
        var result = (await sut.GetUserAnidexAsync(userId)).ToList();

        // Assert
        Assert.That(result.Select(e => e.SpeciesName).ToList(),
            Is.EqualTo(new[] { "Bald Eagle", "Bobcat", "Coyote" }));
    }

    #endregion

    #region CSP-172: GetSightingByIdAsync Tests

    [Test]
    public async Task GetSightingByIdAsync_ExistingId_ReturnsThatSighting()
    {
        // Arrange
        var sightingId = Guid.NewGuid();
        var sighting = new Sighting
        {
            Id = sightingId,
            UserId = Guid.NewGuid(),
            Latitude = 44.0m,
            Longitude = -123.0m,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1),
            Description = "Test sighting",
            ImageBuffer = [0x01],
            SpeciesName = "Coyote"
        };

        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.Is<Guid>(g => g == sightingId)))
            .ReturnsAsync(sighting)
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSightingByIdAsync(sightingId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(sightingId));
        Assert.That(result.SpeciesName, Is.EqualTo("Coyote"));
        AssertAllMockVerifications();
    }

    [Test]
    public async Task GetSightingByIdAsync_UnknownId_ReturnsNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.Is<Guid>(g => g == unknownId)))
            .ReturnsAsync((Sighting?)null)
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSightingByIdAsync(unknownId);

        // Assert
        Assert.That(result, Is.Null);
        AssertAllMockVerifications();
    }

    #endregion

    [Test]
    public async Task CreateSightingAsync_UserHasActiveStreak_Applies1Point5Multiplier()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var basePoints = 100;
        var expectedPoints = 150; // 100 * 1.5

        // Create Sighting tied to the user
        var sighting = SightingValidValuesSource.DefaultValidSighting;
        sighting.UserIdentityId = userId; 

        // Create user with an active streak
        // In ApplicationUser.cs, IsStreakActive is true if LastLogin is within 30 days
        var user = new ApplicationUser
        {
            Id = userId,
            Points = 0,
            LastLogin = DateTimeOffset.UtcNow 
        };

        // Mock a Sighting
        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()))
            .ReturnsAsync(sighting);

        // Mock Sighting scoring
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync(It.IsAny<string>()))
            .ReturnsAsync(10);
        _scoringServiceMock.Setup(s => s.CalculatePointsAsync(It.IsAny<int>()))
            .ReturnsAsync(basePoints);

        // Mock user querying, so IsStreakActive can be accessed.
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser> { user }.AsQueryable());
        _userRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(user);

        var sut = CreateSut();
        var result = await sut.CreateSightingAsync(sighting);

        // Assert
        Assert.Multiple(() =>
        {
            // Verify the multiplied points
            Assert.That(result, Is.EqualTo(expectedPoints), "The service should return 1.5x points when streak is active.");

            // Verify user points were updated correctly
            Assert.That(user.Points, Is.EqualTo(expectedPoints), "The user's Points property should be updated with the multiplier.");

            // Verify user repository was called to save the new point total
            _userRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<ApplicationUser>(u => u.Id == userId && u.Points == expectedPoints)), Times.Once);
        });
    }

    private static IFormFile GenerateBadFormFile(Stream stream, int offset, int len, string filename)
    {
        return new FormFile(stream, offset, len, "file", filename);
    }

    #region CSP-177: Offline Queue Idempotency

    [Test]
    public async Task CreateSightingAsync_WithNullClientId_CreatesSightingNormally()
    {
        // Arrange
        var sighting = _validSighting;
        sighting.ClientSightingId = null;
        var pointsValue = 10;

        _scoringServiceMock.Setup(s => s.CalculatePointsAsync(It.IsAny<int>())).ReturnsAsync(pointsValue);
        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Sighting>().AsQueryable());
        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()))
            .ReturnsAsync(sighting).Verifiable(Times.Once);
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser>
                { new ApplicationUser { Id = sighting.UserIdentityId } }.AsQueryable());
        _userRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new ApplicationUser());

        var sut = CreateSut();

        // Act
        var result = await sut.CreateSightingAsync(sighting);

        // Assert — sighting repo was called to create a new record
        _sightingsRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()), Times.Once);
    }

    [Test]
    public async Task CreateSightingAsync_WithNewClientId_CreatesSighting()
    {
        // Arrange
        var sighting = _validSighting;
        sighting.ClientSightingId = Guid.NewGuid().ToString();
        var pointsValue = 20;

        _scoringServiceMock.Setup(s => s.CalculatePointsAsync(It.IsAny<int>())).ReturnsAsync(pointsValue);
        // No existing sighting with this client ID
        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Sighting>().AsQueryable());
        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()))
            .ReturnsAsync(sighting).Verifiable(Times.Once);
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser>
                { new ApplicationUser { Id = sighting.UserIdentityId } }.AsQueryable());
        _userRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new ApplicationUser());

        var sut = CreateSut();

        // Act
        await sut.CreateSightingAsync(sighting);

        // Assert — a new sighting was persisted
        _sightingsRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()), Times.Once);
    }

    [Test]
    public async Task CreateSightingAsync_WithDuplicateClientId_ReturnsExistingPointsWithoutCreatingDuplicate()
    {
        // Arrange
        var clientId = Guid.NewGuid().ToString();
        var existingPointValue = 50;
        var sighting = _validSighting;
        sighting.ClientSightingId = clientId;

        var existingSighting = new Sighting
        {
            Id = Guid.NewGuid(),
            UserIdentityId = sighting.UserIdentityId,
            ClientSightingId = clientId,
            PointValue = existingPointValue,
            Latitude = 45m,
            Longitude = -123m,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1),
            ImageBuffer = [0x01],
            SpeciesName = "Test Species"
        };

        // Repo returns the existing sighting when queried by ClientSightingId
        _sightingsRepoMock.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync(new List<Sighting> { existingSighting }.AsQueryable());

        var sut = CreateSut();

        // Act
        var result = await sut.CreateSightingAsync(sighting);

        // Assert — returns existing points, no new record added
        Assert.That(result, Is.EqualTo(existingPointValue));
        _sightingsRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()), Times.Never);
    }

    #endregion
}

[ExcludeFromCodeCoverage]
public class SightingComparer : IEqualityComparer<Sighting>
{
    public static IEqualityComparer<Sighting> Instance => new SightingComparer();

    public bool Equals(Sighting? x, Sighting? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;

        return x.Id == y.Id &&
               x.UserId == y.UserId &&
               x.Latitude == y.Latitude &&
               x.Longitude == y.Longitude &&
               x.Description == y.Description &&
               x.Timestamp.Equals(y.Timestamp) &&
               (x.ImageBuffer == null && y.ImageBuffer == null ||
                x.ImageBuffer != null && y.ImageBuffer != null && x.ImageBuffer.SequenceEqual(y.ImageBuffer));
    }

    public int GetHashCode(Sighting obj)
    {
        var hash = new HashCode();
        hash.Add(obj.Id);
        hash.Add(obj.UserId);
        hash.Add(obj.Description);
        hash.Add(obj.Latitude);
        hash.Add(obj.Longitude);
        hash.Add(obj.Timestamp.UtcTicks);
        hash.Add(obj.ImageBuffer);
        return hash.ToHashCode();
    }
}

[ExcludeFromCodeCoverage]
public struct SightingValidValuesSource
{
    public const int _EnumerableCounts = 2;

    private static readonly DateTimeOffset _fixedBaseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public static Sighting DefaultValidSighting =>
        new Sighting(Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, _fixedBaseTime,
            string.Empty, [0x01], 10, false, "Common", 1.0);

    public static IEnumerable<decimal> GetValidLats() =>
            GetEnumerableOfDecimalsInRangeOfAmount(_EnumerableCounts, -90m, 90m);

    public static IEnumerable<decimal> GetValidLongs() =>
            GetEnumerableOfDecimalsInRangeOfAmount(_EnumerableCounts, -180m, 180m);

    public static IEnumerable<string> GetValidDescriptions()
    {
        int maxLength = 500;
        for (int i = 0; i < _EnumerableCounts; i++)
        {
            yield return GetRandomStringOfLength(GetRandomIntInRange(1, maxLength));
        }
    }

    public static IEnumerable<DateTimeOffset> GetValidTimestamps()
    {
        for (int i = 0; i < _EnumerableCounts; i++)
        {
            yield return _fixedBaseTime.AddDays(-i);
        }
    }
}
