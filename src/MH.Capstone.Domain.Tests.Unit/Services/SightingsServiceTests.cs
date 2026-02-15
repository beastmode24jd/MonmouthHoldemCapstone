using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using static MH.Capstone.Tests.SharedInternals.RandomData;
using static MH.Capstone.Tests.SharedInternals.SqlExceptionBuilder;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[ExcludeFromCodeCoverage]
public class SightingsServiceTests
{
    private Sighting _validSighting;
    private Mock<IRepository<Sighting>> _sightingsRepoMock;

    // Remember: Arrange, Act, Assert
    [SetUp]
    public void Setup()
    {
        _validSighting = SightingValidValuesSource.DefaultValidSighting;
        _sightingsRepoMock = new Mock<IRepository<Sighting>>();
    }

    private SightingsService CreateSut() =>
        new (NullLogger<SightingsService>.Instance, _sightingsRepoMock.Object);

    private void AssertAllMockVerifications()
    {

        // Asserts that the methods that were set up in the Moq were called in ways that we set up
        _sightingsRepoMock.VerifyAll();

        // Asserts that the Moq mocks were only called in ways that we set up with the Setup method,
        // failing if any method was called that was not set up
        _sightingsRepoMock.VerifyNoOtherCalls();
    }

    // Will run 2^4 = 16 times, testing all combinations of the valid values for lat, long, timestamp, and description
    [Test]
    public void CreateSightingAsync_ValidSighting_ReturnsSuccessfulTaskWithoutException(
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidLats))] decimal lat,
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidLongs))] decimal lon,
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidTimestamps))] DateTime timestamp,
        [ValueSource(typeof(SightingValidValuesSource), nameof(SightingValidValuesSource.GetValidDescriptions))] string desc)
    {
        // Arrange
        var sighting = new Sighting(_validSighting.Id, _validSighting.UserId, lat, lon, timestamp, desc);

        _sightingsRepoMock.Setup(r => 
            r.AddOrUpdateAsync(It.Is(sighting, SightingComparer.Instance)))
            .ReturnsAsync(sighting).Verifiable(Times.Once);
        
        var sut = CreateSut();

        // Act & Assert
        Assert.DoesNotThrowAsync(() => sut.CreateSightingAsync(sighting));
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
        sighting.Timestamp = DateTime.UtcNow.AddHours(2);

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
}

[ExcludeFromCodeCoverage]
public class SightingComparer : IEqualityComparer<Sighting>
{
    public static IEqualityComparer<Sighting> Instance => new SightingComparer();

    public bool Equals(Sighting? x, Sighting? y)
    {
        // First check for nulls to avoid NullReferenceException when calling GetHashCode
        if (x == null || y == null)
            return false;

        // We can rely on GetHashCode to determine equality since we combine all properties of
        // Sighting in the hash code generation. We use this to simplify the equality check,
        // as two Sightings with the same values will have the same hash code, but sightings could
        // have different references in memory.
        return GetHashCode(x) == GetHashCode(y);
    }

    // We combine all properties of Sighting to generate a hash code,
    // ensuring that two Sightings with the same values will have the same hash code
    public int GetHashCode(Sighting obj)
    {
        return HashCode.Combine(obj.UserId, obj.Description, obj.Id, obj.Latitude, obj.Longitude, obj.Timestamp);
    }
}

[ExcludeFromCodeCoverage]
public struct SightingValidValuesSource
{
    public const int _EnumerableCounts = 2;

    public static Sighting DefaultValidSighting =>
        new Sighting(Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, DateTime.UtcNow, string.Empty);

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

    public static IEnumerable<DateTime> GetValidTimestamps()
    {
        for (int i = 0; i < _EnumerableCounts; i++)
        {
            // Generate a random DateTime in the past year
            yield return DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365));
        }
    }
}