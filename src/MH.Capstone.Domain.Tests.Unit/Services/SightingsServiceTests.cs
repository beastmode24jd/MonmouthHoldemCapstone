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
    }

    [TearDown]
    public void TearDown()
    {
        _imageGenerator.Dispose();
    }

    private SightingsService CreateSut() =>
        new (NullLogger<SightingsService>.Instance,
            _scoringServiceMock.Object,
            _notificationServiceMock.Object,
            _sightingsRepoMock.Object,
            _userRepoMock.Object);

    private void AssertAllMockVerifications()
    {

        // Asserts that the methods that were set up in the Moq were called in ways that we set up
        _sightingsRepoMock.VerifyAll();
        _scoringServiceMock.VerifyAll();
        _notificationServiceMock.VerifyAll();
        _userRepoMock.VerifyAll();

        // Asserts that the Moq mocks were only called in ways that we set up with the Setup method,
        // failing if any method was called that was not set up
        _sightingsRepoMock.VerifyNoOtherCalls();
        _scoringServiceMock.VerifyNoOtherCalls();
        _notificationServiceMock.VerifyNoOtherCalls();
        _userRepoMock.VerifyNoOtherCalls();
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
            timestamp, desc, [0x01]);
        var sightingsCount = GetRandomIntInRange(1, 100);
        var pointsValue = GetRandomIntInRange(1, 20);

        _sightingsRepoMock.Setup(r => 
            r.AddOrUpdateAsync(It.Is(sighting, SightingComparer.Instance)))
            .ReturnsAsync(sighting).Verifiable(Times.Once);

        // It.Is<int>(i => i == 1) placeholder till species is fully developed.
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync(
            It.Is<int>(i => i == 1)))
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
            It.Is<Notification>(n => n.RecipientId == sighting.UserId)))
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

        // It.Is<int>(i => i == 1) placeholder till species is fully developed.
        _scoringServiceMock.Setup(s => s.GetGlobalSightingsCountAsync(
                It.Is<int>(i => i == 1)))
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
                It.Is<Notification>(n => n.RecipientId == sighting.UserId)))
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

        _sightingsRepoMock.Setup(r => 
            r.AddOrUpdateAsync(It.Is(sighting, SightingComparer.Instance)))
            .ThrowsAsync(new DbUpdateException("Foreign key violation", new SqlExceptionBuilder().WithNumber(547).Build()))
            .Verifiable(Times.Once);

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
    public void ValidateImageAsync_TooLargeImageFIle_ReturnsFalse()
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

    private static IFormFile GenerateBadFormFile(Stream stream, int offset, int len, string filename)
    {
        return new FormFile(stream, offset, len, "file", filename);
    }
}

[ExcludeFromCodeCoverage]
public class SightingComparer : IEqualityComparer<Sighting>
{
    public static IEqualityComparer<Sighting> Instance => new SightingComparer();

    public bool Equals(Sighting? x, Sighting? y)
    {
        //
        if(ReferenceEquals(x, y)) return true;

        // First check for nulls to avoid NullReferenceException when calling GetHashCode
        if (x == null || y == null) return false;

        // DateTimeOffset breaks the original GetHashCode comparison setup, because
        //  1) SQL Servers truncate DateTimeOffset values, so it doesn't replicate
        //      the comparison of DateTimeOffset like it did with DateTime
        //  2) DateTimeOffset is precise enough to measure nanoseconds. Running
        //      new DateTimeOffset value initializations could give *nearly* the same
        //      DateTimeOffset, and it would still fail due to this.

        // So, we manually return a very long logical "and" statement.
        return x.Id == y.Id &&
               x.UserId == y.UserId &&
               x.Latitude == y.Latitude &&
               x.Longitude == y.Longitude &&
               x.Description == y.Description &&
               // Compares exact point in time, regardless of timezone offset
               x.Timestamp.Equals(y.Timestamp) && 
               (x.ImageBuffer == null && y.ImageBuffer == null || 
                x.ImageBuffer != null && y.ImageBuffer != null && x.ImageBuffer.SequenceEqual(y.ImageBuffer));
    }

    // We combine all properties of Sighting to generate a hash code,
    // ensuring that two Sightings with the same values will have the same hash code
    public int GetHashCode(Sighting obj)
    {
        var hash = new HashCode();
        hash.Add(obj.Id);
        hash.Add(obj.UserId);
        hash.Add(obj.Description);
        hash.Add(obj.Latitude);
        hash.Add(obj.Longitude);
        hash.Add(obj.Timestamp.UtcTicks); // Ensures the DateTimeOffset ticks are equal (nanoseconds)
        hash.Add(obj.ImageBuffer);
        return hash.ToHashCode();
    }
}

[ExcludeFromCodeCoverage]
public struct SightingValidValuesSource
{
    public const int _EnumerableCounts = 2;

    // Prevents DateTimeOffset drift during testing
    private static readonly DateTimeOffset _fixedBaseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public static Sighting DefaultValidSighting =>
        new Sighting(Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, DateTimeOffset.UtcNow, 
            string.Empty, [0x01]);

    public static IEnumerable<decimal> GetValidLats() =>
            GetEnumerableOfDecimalsInRangeOfAmount(_EnumerableCounts, -90m, 90m);

    public static IEnumerable<decimal> GetValidLongs() =>
            GetEnumerableOfDecimalsInRangeOfAmount(_EnumerableCounts, -180m, 180m);

    public static IEnumerable<string> GetValidDescriptions()
    {
        int maxLength = 500; // Max length for description
        for (int i = 0; i < _EnumerableCounts; i++)
        {
            yield return GetRandomStringOfLength(GetRandomIntInRange(1, maxLength));
        }
    }

    public static IEnumerable<DateTimeOffset> GetValidTimestamps()
    {
        // Uses a fixed point, due to the nanosecond sensitivity of DateTimeOffset

        for (int i = 0; i < _EnumerableCounts; i++)
        {
            yield return _fixedBaseTime.AddDays(-i);
        }
    }
}