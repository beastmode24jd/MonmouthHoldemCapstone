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
    private Mock<ILiveBroadcastService> _liveBroadcastMock;
    private Mock<ILeaderboardService> _leaderboardServiceMock;
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
        _liveBroadcastMock = new Mock<ILiveBroadcastService>();
        _liveBroadcastMock
            .Setup(b => b.BroadcastLeaderboardUpdateAsync(It.IsAny<LeaderboardEntryUpdate>()))
            .Returns(Task.CompletedTask);
        _leaderboardServiceMock = new Mock<ILeaderboardService>();
        _leaderboardServiceMock
            .Setup(s => s.GetUserRankAsync(It.IsAny<string>()))
            .ReturnsAsync(1);

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
            _badgeServiceMock.Object,
            _liveBroadcastMock.Object,
            _leaderboardServiceMock.Object);

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

    // Sets up the repo's predicate overload to actually evaluate the expression against
    // a backing list, so these tests genuinely exercise the bounds filter the service builds
    // (the previous setups mocked the parameterless GetAllAsync(), which the service no longer
    // calls — they passed vacuously regardless of the filter logic).
    private void SetupBoundsRepo(params Sighting[] backing) =>
        _sightingsRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Sighting, bool>>>()))
            .ReturnsAsync((Expression<Func<Sighting, bool>> predicate) =>
                backing.Where(predicate.Compile()).AsQueryable());

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

        SetupBoundsRepo(sightingInBounds);

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

        SetupBoundsRepo(sightingOutOfBounds);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSightingsInBoundsAsync(44.0m, 46.0m, -124.0m, -122.0m);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    // [CSP-224] Regression guard: an in-bounds sighting older than a week must STILL be
    // returned. The map previously dropped anything older than 7 days, so sightings the
    // user knew existed vanished as they panned/zoomed away from recent activity.
    [Test]
    public async Task GetSightingsInBoundsAsync_IncludesInBoundsSightings_RegardlessOfAge()
    {
        // Arrange
        var oldInBounds = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 45.0m,
            Longitude = -123.0m,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-400),  // well over a year old
            Description = "Old but in-bounds sighting",
            ImageBuffer = new byte[] { 0x01 }
        };

        SetupBoundsRepo(oldInBounds);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSightingsInBoundsAsync(44.0m, 46.0m, -124.0m, -122.0m);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Id, Is.EqualTo(oldInBounds.Id));
    }

    // [CSP-224] With a mixed set, only the in-bounds sightings come back — old or recent —
    // and the out-of-bounds one is excluded purely on geography.
    [Test]
    public async Task GetSightingsInBoundsAsync_ReturnsAllInBounds_IgnoringAge_ExcludingOutOfBounds()
    {
        // Arrange
        var recentInBounds = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 45.5m, Longitude = -123.2m,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            Description = "recent in-bounds", ImageBuffer = new byte[] { 0x01 }
        };
        var oldInBounds = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 44.2m, Longitude = -122.5m,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-90),
            Description = "old in-bounds", ImageBuffer = new byte[] { 0x01 }
        };
        var outOfBounds = new Sighting
        {
            Id = Guid.NewGuid(),
            Latitude = 34.0m, Longitude = -118.0m,  // LA — outside the box
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            Description = "out of bounds", ImageBuffer = new byte[] { 0x01 }
        };

        SetupBoundsRepo(recentInBounds, oldInBounds, outOfBounds);

        var sut = CreateSut();

        // Act
        var result = (await sut.GetSightingsInBoundsAsync(44.0m, 46.0m, -124.0m, -122.0m)).ToList();

        // Assert
        Assert.That(result.Select(s => s.Id), Is.EquivalentTo(new[] { recentInBounds.Id, oldInBounds.Id }));
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

    [Test]
    public void CreateSightingAsync_BroadcastThrows_SightingStillSucceeds()
    {
        // Arrange — broadcast is wired to throw; sighting should still commit and return points
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, DisplayName = "Alex", Points = 0 };

        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser> { user }.AsQueryable());
        _scoringServiceMock.Setup(s => s.CalculatePointsAsync(It.IsAny<int>())).ReturnsAsync(10);
        _liveBroadcastMock
            .Setup(b => b.BroadcastLeaderboardUpdateAsync(It.IsAny<LeaderboardEntryUpdate>()))
            .ThrowsAsync(new InvalidOperationException("SignalR down"));

        var sighting = _validSighting;
        sighting.UserIdentityId = userId;
        var sut = CreateSut();

        // Act & Assert — must not throw despite broadcast failure
        Assert.DoesNotThrowAsync(() => sut.CreateSightingAsync(sighting));
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

    #region CSP-199: GetSightingsPageAsync Pagination Tests

    // Builds `count` sightings with strictly increasing timestamps (s0 = oldest,
    // s{count-1} = newest). Input order is oldest-first on purpose so the tests
    // prove the service applies the descending sort itself.
    private static List<Sighting> BuildSightings(int count)
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var list = new List<Sighting>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new Sighting
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Timestamp = baseTime.AddMinutes(i),
                Description = $"s{i}",
                ImageBuffer = [0x01]
            });
        }
        return list;
    }

    // Pins: a full page returns exactly pageSize items, TotalCount reflects the
    // WHOLE dataset (not the page), and paging metadata is correct.
    [Test]
    public async Task GetSightingsPageAsync_FirstPage_ReturnsAtMostPageSizeItemsAndFullTotalCount()
    {
        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(BuildSightings(25).AsQueryable());
        var sut = CreateSut();

        var result = await sut.GetSightingsPageAsync(page: 1, pageSize: 20);

        Assert.That(result.Items.Count, Is.EqualTo(20));
        Assert.That(result.TotalCount, Is.EqualTo(25));
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(20));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.HasPreviousPage, Is.False);
        Assert.That(result.HasNextPage, Is.True);
        // Newest sighting (s24) is first because of the descending sort.
        Assert.That(result.Items[0].Description, Is.EqualTo("s24"));
    }

    // Pins: the last page returns only the leftover items, not a full page.
    [Test]
    public async Task GetSightingsPageAsync_SecondPage_ReturnsOnlyRemainingItems()
    {
        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(BuildSightings(25).AsQueryable());
        var sut = CreateSut();

        var result = await sut.GetSightingsPageAsync(page: 2, pageSize: 20);

        Assert.That(result.Items.Count, Is.EqualTo(5));
        Assert.That(result.Page, Is.EqualTo(2));
        Assert.That(result.HasPreviousPage, Is.True);
        Assert.That(result.HasNextPage, Is.False);
        // Page 1 held s24..s5; page 2 holds the 5 oldest, newest-first: s4..s0.
        Assert.That(result.Items[0].Description, Is.EqualTo("s4"));
        Assert.That(result.Items[^1].Description, Is.EqualTo("s0"));
    }

    // Pins: items within a page are ordered most-recent-first.
    [Test]
    public async Task GetSightingsPageAsync_ReturnsItemsOrderedByTimestampDescending()
    {
        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(BuildSightings(5).AsQueryable());
        var sut = CreateSut();

        var result = await sut.GetSightingsPageAsync(page: 1, pageSize: 20);

        Assert.That(result.Items.Count, Is.EqualTo(5));
        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            Assert.That(result.Items[i].Timestamp,
                Is.GreaterThan(result.Items[i + 1].Timestamp));
        }
    }

    // Pins: asking for a page past the end yields no items but still reports the
    // true total (so the UI can render "page X of Y" / disable Next correctly).
    [Test]
    public async Task GetSightingsPageAsync_PageBeyondLastPage_ReturnsEmptyItemsButCorrectTotalCount()
    {
        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(BuildSightings(5).AsQueryable());
        var sut = CreateSut();

        var result = await sut.GetSightingsPageAsync(page: 3, pageSize: 20);

        Assert.That(result.Items, Is.Empty);
        Assert.That(result.TotalCount, Is.EqualTo(5));
        Assert.That(result.HasNextPage, Is.False);
    }

    // Pins: empty dataset is a valid empty page, not a crash.
    [Test]
    public async Task GetSightingsPageAsync_NoSightings_ReturnsEmptyResultWithZeroTotal()
    {
        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(Enumerable.Empty<Sighting>().AsQueryable());
        var sut = CreateSut();

        var result = await sut.GetSightingsPageAsync(page: 1, pageSize: 20);

        Assert.That(result.Items, Is.Empty);
        Assert.That(result.TotalCount, Is.EqualTo(0));
        Assert.That(result.TotalPages, Is.EqualTo(0));
        Assert.That(result.HasNextPage, Is.False);
        Assert.That(result.HasPreviousPage, Is.False);
    }

    // Pins: a bad/low page number (0, negative) is clamped to page 1 rather than
    // returning an empty/garbage page from a negative Skip.
    [Test]
    public async Task GetSightingsPageAsync_PageLessThanOne_ClampsToFirstPage()
    {
        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(BuildSightings(25).AsQueryable());
        var sut = CreateSut();

        var result = await sut.GetSightingsPageAsync(page: 0, pageSize: 20);

        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.Items.Count, Is.EqualTo(20));
        Assert.That(result.Items[0].Description, Is.EqualTo("s24"));
    }

    // Pins: consecutive pages are disjoint and together cover every sighting
    // exactly once, in unbroken descending order.
    [Test]
    public async Task GetSightingsPageAsync_ConsecutivePages_DoNotOverlapAndCoverAllSightings()
    {
        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(BuildSightings(25).AsQueryable());
        var sut = CreateSut();

        var page1 = await sut.GetSightingsPageAsync(page: 1, pageSize: 10);
        var page2 = await sut.GetSightingsPageAsync(page: 2, pageSize: 10);
        var page3 = await sut.GetSightingsPageAsync(page: 3, pageSize: 10);

        var combined = page1.Items.Concat(page2.Items).Concat(page3.Items)
            .Select(s => s.Description).ToList();

        Assert.That(page1.Items.Count, Is.EqualTo(10));
        Assert.That(page2.Items.Count, Is.EqualTo(10));
        Assert.That(page3.Items.Count, Is.EqualTo(5));
        Assert.That(combined.Distinct().Count(), Is.EqualTo(25));
        // Full descending sweep: s24, s23, ..., s0
        var expected = Enumerable.Range(0, 25).Reverse().Select(i => $"s{i}");
        Assert.That(combined, Is.EqualTo(expected));
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

    #region CSP-37: UpdateSightingAsync Tests

    // Builds a persisted-looking sighting owned by `ownerId` with known scoring/immutable fields
    // so each test can assert exactly what the edit operation does and does not touch.
    private static Sighting BuildOwnedSighting(Guid sightingId, Guid ownerId) => new()
    {
        Id = sightingId,
        UserId = ownerId,
        Latitude = 44.5m,
        Longitude = -123.25m,
        Timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
        Description = "Original description",
        SpeciesName = "Coyote",
        ImageBuffer = [0x01, 0x02, 0x03],
        PointValue = 50,
        Rarity = "Mythic",
        RarityMultiplier = 5.0
    };

    [Test]
    public async Task UpdateSightingAsync_OwnerValidEdit_UpdatesDescriptionAndSpeciesNameAndSaves()
    {
        // Arrange
        var sightingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var sighting = BuildOwnedSighting(sightingId, ownerId);

        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.Is<Guid>(g => g == sightingId)))
            .ReturnsAsync(sighting).Verifiable(Times.Once);
        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.Is<Sighting>(s => s.Id == sightingId)))
            .ReturnsAsync(sighting).Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateSightingAsync(sightingId, ownerId, "Updated description", "Gray Wolf");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Description, Is.EqualTo("Updated description"));
        Assert.That(result.SpeciesName, Is.EqualTo("Gray Wolf"));
        AssertAllMockVerifications();
    }

    [Test]
    public async Task UpdateSightingAsync_OwnerValidEdit_DoesNotRecalculateScoring()
    {
        // Arrange — scoring service must never be consulted on edit, and the frozen
        // scoring fields must come through unchanged.
        var sightingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var sighting = BuildOwnedSighting(sightingId, ownerId);

        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(sighting);
        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Sighting>())).ReturnsAsync(sighting);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateSightingAsync(sightingId, ownerId, "New desc", "New species");

        // Assert
        Assert.That(result!.PointValue, Is.EqualTo(50));
        Assert.That(result.Rarity, Is.EqualTo("Mythic"));
        Assert.That(result.RarityMultiplier, Is.EqualTo(5.0));
        _scoringServiceMock.Verify(s => s.GetGlobalSightingsCountAsync(It.IsAny<string>()), Times.Never);
        _scoringServiceMock.Verify(s => s.CalculatePointsAsync(It.IsAny<int>()), Times.Never);
        _scoringServiceMock.Verify(s => s.GetRarityMultiplierAndName(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task UpdateSightingAsync_OwnerValidEdit_DoesNotModifyImmutableFields()
    {
        // Arrange
        var sightingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var sighting = BuildOwnedSighting(sightingId, ownerId);
        var originalImage = sighting.ImageBuffer;

        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(sighting);
        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Sighting>())).ReturnsAsync(sighting);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateSightingAsync(sightingId, ownerId, "New desc", "New species");

        // Assert — GPS, timestamp, and photo are part of the factual record and stay frozen.
        Assert.That(result!.Latitude, Is.EqualTo(44.5m));
        Assert.That(result.Longitude, Is.EqualTo(-123.25m));
        Assert.That(result.Timestamp, Is.EqualTo(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));
        Assert.That(result.ImageBuffer, Is.EqualTo(originalImage));
    }

    [Test]
    public async Task UpdateSightingAsync_UnknownId_ReturnsNullAndDoesNotSave()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.Is<Guid>(g => g == unknownId)))
            .ReturnsAsync((Sighting?)null).Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateSightingAsync(unknownId, Guid.NewGuid(), "desc", "species");

        // Assert
        Assert.That(result, Is.Null);
        _sightingsRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()), Times.Never);
        AssertAllMockVerifications();
    }

    [Test]
    public void UpdateSightingAsync_NonOwner_ThrowsUnauthorizedAndDoesNotSave()
    {
        // Arrange — sighting exists but belongs to someone else.
        var sightingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var sighting = BuildOwnedSighting(sightingId, ownerId);

        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(sighting);

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.UpdateSightingAsync(sightingId, attackerId, "hijacked", "Fake Species"));
        _sightingsRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Sighting>()), Times.Never);
    }

    [Test]
    public async Task UpdateSightingAsync_NoActualChange_SavesWithoutError()
    {
        // Arrange — submitting the same values is a valid idempotent no-op edit.
        var sightingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var sighting = BuildOwnedSighting(sightingId, ownerId);

        _sightingsRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(sighting);
        _sightingsRepoMock.Setup(r => r.AddOrUpdateAsync(It.IsAny<Sighting>())).ReturnsAsync(sighting);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateSightingAsync(sightingId, ownerId, "Original description", "Coyote");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Description, Is.EqualTo("Original description"));
        Assert.That(result.SpeciesName, Is.EqualTo("Coyote"));
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
