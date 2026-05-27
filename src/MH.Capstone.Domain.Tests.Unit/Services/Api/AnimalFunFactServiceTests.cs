using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.ApiContracts.Ninja;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MH.Capstone.Domain.Tests.Unit.Services.Api;

[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class AnimalFunFactServiceTests
{
    private const string AnimalEndpointPath = "/v1/animals";

    private Mock<IApiCaller<NinjaApiConfigValues>> _ninjaApiCallerMock = null!;
    private NinjaApiConfigValues _config = null!;

    [SetUp]
    public void Setup()
    {
        _ninjaApiCallerMock = new Mock<IApiCaller<NinjaApiConfigValues>>();
        _config = new NinjaApiConfigValues(
            httpClientKey: "ninja",
            baseUrl: "https://api.example/",
            endpoints: new[]
            {
                new KeyValuePair<string, string>("animal", AnimalEndpointPath)
            });

        _ninjaApiCallerMock.Setup(c => c.ConfigValues).Returns(_config);
    }

    private AnimalFunFactService CreateSut() =>
        new(NullLogger<AnimalFunFactService>.Instance, _ninjaApiCallerMock.Object);

    private static AnimalApiDto MakeDto(
        string name = "Coyote",
        string slogan = "",
        string mostDistinctiveFeature = "",
        string lifestyle = "")
    {
        var taxonomy = new AnimalApiTaxonomyDto(
            kingdom: "Animalia", phylum: "Chordata", taxClass: "Mammalia",
            order: "Carnivora", family: "Canidae", genus: "Canis",
            scientificName: "Canis latrans");

        var characteristics = new AnimalApiCharacteristics(
            prey: "", nameOfYoung: "", groupBehavior: "",
            estimatedPopulationSize: "", biggestThreat: "",
            mostDistinctiveFeature: mostDistinctiveFeature,
            gestationPeriod: "", habitat: "", diet: "",
            averageLitterSize: "", lifestyle: lifestyle,
            commonName: "", numberOfSpecies: "", location: "",
            slogan: slogan, group: "", color: "", skinType: "",
            topSpeed: "", lifespan: "", weight: "", height: "",
            ageOfSexualMaturity: "", ageOfWeaning: "");

        return new AnimalApiDto(name, taxonomy, locations: new[] { "" }, characteristics);
    }

    [Test]
    public async Task GetFunFactAsync_DtoHasSlogan_ReturnsTheSlogan()
    {
        // Arrange
        var dto = MakeDto(slogan: "The trickster of the American West!");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.Is<string>(u => u == AnimalEndpointPath),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(new[] { dto });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert
        Assert.That(result, Is.EqualTo("The trickster of the American West!"));
    }

    [Test]
    public async Task GetFunFactAsync_SloganEmpty_FallsBackToMostDistinctiveFeature()
    {
        // Arrange
        var dto = MakeDto(slogan: "", mostDistinctiveFeature: "Bushy black-tipped tail.");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(new[] { dto });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert
        Assert.That(result, Is.EqualTo("Bushy black-tipped tail."));
    }

    [Test]
    public async Task GetFunFactAsync_SloganAndDistinctiveFeatureEmpty_FallsBackToLifestyle()
    {
        // Arrange
        var dto = MakeDto(slogan: "", mostDistinctiveFeature: "", lifestyle: "Nocturnal");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(new[] { dto });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert
        Assert.That(result, Is.EqualTo("Nocturnal"));
    }

    [Test]
    public async Task GetFunFactAsync_AllCandidatesEmpty_ReturnsNull()
    {
        // Arrange
        var dto = MakeDto(slogan: "", mostDistinctiveFeature: "", lifestyle: "");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(new[] { dto });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetFunFactAsync_FirstMatchHasNoUsableFact_FallsBackToLaterMatch()
    {
        // Arrange — CSP-214 regression. The API-Ninjas animals endpoint returns several
        // fuzzy name matches. The first match is a sparse entry with no usable
        // characteristics, but a later match carries a slogan. The service must scan all
        // matches rather than giving up after the first one (the cause of fun facts only
        // working for some species).
        var sparseFirst = MakeDto(name: "Coyote (subspecies)", slogan: "", mostDistinctiveFeature: "", lifestyle: "");
        var richSecond = MakeDto(name: "Coyote", slogan: "The trickster of the American West!");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(new[] { sparseFirst, richSecond });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert
        Assert.That(result, Is.EqualTo("The trickster of the American West!"));
    }

    [Test]
    public async Task GetFunFactAsync_FirstMatchHasNullCharacteristics_FallsBackToLaterMatch()
    {
        // Arrange — defensive variant of the CSP-214 fix: a match can come back with no
        // characteristics object at all; that must be skipped, not treated as "no fact".
        var nullCharacteristicsFirst = new AnimalApiDto(
            "Coyote", taxonomy: null!, locations: new[] { "" }, characteristics: null!);
        var richSecond = MakeDto(name: "Coyote", slogan: "The trickster of the American West!");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(new[] { nullCharacteristicsFirst, richSecond });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert
        Assert.That(result, Is.EqualTo("The trickster of the American West!"));
    }

    [Test]
    public async Task GetFunFactAsync_NoMatchHasUsableFact_ReturnsNull()
    {
        // Arrange — when every returned match is sparse, the fallback message is correct.
        var sparseA = MakeDto(name: "Critter A", slogan: "", mostDistinctiveFeature: "", lifestyle: "");
        var sparseB = MakeDto(name: "Critter B", slogan: "", mostDistinctiveFeature: "", lifestyle: "");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(new[] { sparseA, sparseB });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Critter");

        // Assert
        Assert.That(result, Is.Null);
    }

    // Matches the GetAsync call whose "name" query param equals the expected value (case-insensitive).
    private static bool QueriesName(IEnumerable<KeyValuePair<string, string>>? queryParams, string expected) =>
        queryParams != null &&
        queryParams.Any(kvp => kvp.Key == "name" &&
                               string.Equals(kvp.Value, expected, StringComparison.OrdinalIgnoreCase));

    [Test]
    public async Task GetFunFactAsync_VerbatimNameReturnsNothing_RetriesWithSimplerWord()
    {
        // Arrange — CSP-214 (mallard duck). The recorded species name is "Mallard Duck", but the
        // Animals API only knows "Mallard": the verbatim query returns nothing, while the simpler
        // single-word query resolves. The service must fall back to a simpler query rather than
        // surfacing the "not available" message.
        var mallard = MakeDto(
            name: "Mallard",
            mostDistinctiveFeature: "The iridescent green or blue-headed plumage of the male");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.Is<IEnumerable<KeyValuePair<string, string>>>(q => QueriesName(q, "Mallard Duck"))))
            .ReturnsAsync(Array.Empty<AnimalApiDto>());

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.Is<IEnumerable<KeyValuePair<string, string>>>(q => QueriesName(q, "Mallard"))))
            .ReturnsAsync(new[] { mallard });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Mallard Duck");

        // Assert
        Assert.That(result, Is.EqualTo("The iridescent green or blue-headed plumage of the male"));
    }

    [Test]
    public async Task GetFunFactAsync_VerbatimNameYieldsFact_DoesNotQuerySimplerWords()
    {
        // Arrange — when the verbatim name already resolves, no extra API calls should be made.
        var dto = MakeDto(name: "Bald Eagle", slogan: "The national bird of the United States!");

        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.Is<IEnumerable<KeyValuePair<string, string>>>(q => QueriesName(q, "Bald Eagle"))))
            .ReturnsAsync(new[] { dto });

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Bald Eagle");

        // Assert
        Assert.That(result, Is.EqualTo("The national bird of the United States!"));
        _ninjaApiCallerMock.Verify(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.Is<IEnumerable<KeyValuePair<string, string>>>(q => QueriesName(q, "Eagle"))),
            Times.Never,
            "a successful verbatim lookup must not trigger simpler-word retries");
    }

    [Test]
    public async Task GetFunFactAsync_ApiReturnsEmptyList_ReturnsNull()
    {
        // Arrange
        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ReturnsAsync(Array.Empty<AnimalApiDto>());

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Mystery Critter Z");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetFunFactAsync_ApiThrowsHttpRequestException_ReturnsNull()
    {
        // Arrange
        _ninjaApiCallerMock
            .Setup(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
            .ThrowsAsync(new HttpRequestException("upstream is down"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert — must not propagate; details page renders fallback instead.
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetFunFactAsync_UnknownSpeciesSentinel_ShortCircuitsWithoutApiCall()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Unknown");

        // Assert
        Assert.That(result, Is.Null);
        _ninjaApiCallerMock.Verify(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>()),
            Times.Never,
            "Unknown species should never trigger an API call");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task GetFunFactAsync_NullOrWhitespaceSpecies_ShortCircuitsWithoutApiCall(string? speciesName)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync(speciesName!);

        // Assert
        Assert.That(result, Is.Null);
        _ninjaApiCallerMock.Verify(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>()),
            Times.Never,
            "null/empty species should never trigger an API call");
    }

    [Test]
    public async Task GetFunFactAsync_AnimalEndpointMissingFromConfig_ReturnsNullWithoutApiCall()
    {
        // Arrange — replace config with one that lacks the "animal" endpoint
        var configWithoutAnimalEndpoint = new NinjaApiConfigValues(
            httpClientKey: "ninja",
            baseUrl: "https://api.example/",
            endpoints: Array.Empty<KeyValuePair<string, string>>());

        _ninjaApiCallerMock.Setup(c => c.ConfigValues).Returns(configWithoutAnimalEndpoint);

        var sut = CreateSut();

        // Act
        var result = await sut.GetFunFactAsync("Coyote");

        // Assert — defensive: don't blow up on misconfiguration
        Assert.That(result, Is.Null);
        _ninjaApiCallerMock.Verify(c => c.GetAsync<IEnumerable<AnimalApiDto>>(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>()),
            Times.Never);
    }
}
