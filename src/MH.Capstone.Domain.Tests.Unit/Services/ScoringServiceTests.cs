using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class ScoringServiceTests
{
    private Mock<IRepository<Sighting, ApplicationDbContext>> _sightingsRepoMock = null!;

    // Remember: Arrange, Act, Assert
    [SetUp]
    public void Setup()
    {
        _sightingsRepoMock = new Mock<IRepository<Sighting, ApplicationDbContext>>();
    }

    private ScoringService CreateSut() =>
        new(NullLogger<ScoringService>.Instance, _sightingsRepoMock.Object);


        // Want to add 3 different Tiers for each scoring tier (Mythic, Rare, Common) and then edge case test for negative sightings cound.
        // Testing each tier with 3 different counts to make sure the correct points are returned for each tier and that the edge case is handled accordingly.

    #region Mythic Tier Tests (≤5 sightings = 50 points)

    [Test]
    public async Task CalculatePointsAsync_ZeroSightings_Returns50Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 0;
        const int expectedPoints = 50; // 10 × 5.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    [Test]
    public async Task CalculatePointsAsync_ThreeSightings_Returns50Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 3;
        const int expectedPoints = 50; // 10 × 5.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    [Test]
    public async Task CalculatePointsAsync_FiveSightings_Returns50Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 5;
        const int expectedPoints = 50; // 10 × 5.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    #endregion

    #region Rare Tier Tests (≤50 sightings = 20 points)

    [Test]
    public async Task CalculatePointsAsync_SixSightings_Returns20Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 6;
        const int expectedPoints = 20; // 10 × 2.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    [Test]
    public async Task CalculatePointsAsync_TwentyFiveSightings_Returns20Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 25;
        const int expectedPoints = 20; // 10 × 2.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    [Test]
    public async Task CalculatePointsAsync_FiftySightings_Returns20Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 50;
        const int expectedPoints = 20; // 10 × 2.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    #endregion

    #region Common Tier Tests (>50 sightings = 10 points)

    [Test]
    public async Task CalculatePointsAsync_FiftyOneSightings_Returns10Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 51;
        const int expectedPoints = 10; // 10 × 1.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    [Test]
    public async Task CalculatePointsAsync_OneHundredSightings_Returns10Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 100;
        const int expectedPoints = 10; // 10 × 1.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    [Test]
    public async Task CalculatePointsAsync_OneThousandSightings_Returns10Points()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = 1000;
        const int expectedPoints = 10; // 10 × 1.0

        // Act
        int actualPoints = await sut.CalculatePointsAsync(globalCount);

        // Assert
        Assert.That(actualPoints, Is.EqualTo(expectedPoints));
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void CalculatePointsAsync_NegativeCount_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = -1;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await sut.CalculatePointsAsync(globalCount));
    }

    #endregion

    #region Sighting Metadata tests (CSP-140)

    [TestCase(1)] // Mythic Sighting [≤5 sightings: Mythic (5.0×)]
    [TestCase(25)] // Rare Sighting [≤50 sightings: Rare (2.0×)]
    [TestCase(51)] // Common Sighting [>50 sightings: Common (1.0×)]
    public async Task GetRarityMultiplerAndName_DifferentRarityScores_ReturnsExpected(int input)
    {
        // Arrange
        var sut = CreateSut();

        double expectedMultiplier;
        string expectedRarity;

        // Initialize the expected Rarity names and double multiplers.
        (expectedRarity, expectedMultiplier) = input switch
        {
            <= 5 => ("Mythic", 5.0),
            <= 50 => ("Rare", 2.0),
            _ => ("Common", 1.0)
        };

        // Act
        var (actualMultiplier, actualRarity) = await sut.GetRarityMultiplierAndName(input);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(actualMultiplier, Is.EqualTo(expectedMultiplier), $"Multipler {actualMultiplier} did not match expected range for {expectedRarity}.");
            Assert.That(actualRarity, Is.EqualTo(expectedRarity), $"Rarity of {expectedRarity} expected, rarity of {actualRarity} was given.");
        });
    }

    [Test]
    public void GetRarityMultiplierAndName_InvalidGlobalCount_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        const int globalCount = -1;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await sut.GetRarityMultiplierAndName(globalCount));
    }

    #endregion
}
