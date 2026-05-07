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
