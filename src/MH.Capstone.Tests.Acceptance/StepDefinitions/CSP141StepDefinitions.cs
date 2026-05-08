using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using Reqnroll;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "scoring")]
[ExcludeFromCodeCoverage]
public class CSP141StepDefinitions
{
    private readonly IWebDriver _webDriver;
    private readonly SightingsDriver _sightingsDriver;
    private readonly AnidexDriver _anidexDriver;

    private string? _speciesName;
    private string? _tempImagePath;

    public CSP141StepDefinitions(
        IWebDriver webDriver,
        SightingsDriver sightingsDriver,
        AnidexDriver anidexDriver)
    {
        _webDriver = webDriver;
        _sightingsDriver = sightingsDriver;
        _anidexDriver = anidexDriver;
    }

    [BeforeScenario("scoring")]
    public void ResetSession()
    {
        _webDriver.Manage().Cookies.DeleteAllCookies();
    }

    [AfterScenario("scoring")]
    public void CleanupTempImage()
    {
        if (_tempImagePath != null && File.Exists(_tempImagePath))
            File.Delete(_tempImagePath);
    }

    [When("Alex submits a sighting of a brand new unique species")]
    public void WhenAlexSubmitsASightingOfABrandNewUniqueSpecies()
    {
        _speciesName = $"CSP141-{Guid.NewGuid():N}";
        SubmitSightingWithSpecies(_speciesName);
    }

    [When("Alex submits a sighting with species name {string}")]
    public void WhenAlexSubmitsASightingWithSpeciesName(string speciesName)
    {
        _speciesName = speciesName;
        SubmitSightingWithSpecies(speciesName);
    }

    [Then("the new species appears in Alex's Anidex with a {string} rarity badge")]
    public void ThenTheNewSpeciesAppearsInAnidexWithRarityBadge(string expectedRarity)
    {
        _anidexDriver.NavigateToAnidex();
        var actual = _anidexDriver.GetRarityFor(_speciesName!);
        actual.Should().Be(expectedRarity, $"species '{_speciesName}' has 0 prior sightings so must score as {expectedRarity}");
    }

    [Then("the species entry in Alex's Anidex shows a discovery count of {int}")]
    public void ThenTheSpeciesEntryShowsADiscoveryCountOf(int expectedCount)
    {
        _anidexDriver.NavigateToAnidex();
        // "GREAT BLUE HERON" merges with the seeded "Great Blue Heron" entries via case-insensitive grouping.
        var actual = _anidexDriver.GetDiscoveryCountFor(_speciesName!);
        actual.Should().Be(expectedCount, $"submitting '{_speciesName}' should merge with 2 seeded 'Great Blue Heron' sightings");
    }

    [Then("the species entry shows a {string} rarity badge")]
    public void ThenTheSpeciesEntryShowsARarityBadge(string expectedRarity)
    {
        // Browser is already on the Anidex page from the preceding Then step.
        var actual = _anidexDriver.GetRarityFor(_speciesName!);
        actual.Should().Be(expectedRarity, $"2 prior sightings of that species is within the Mythic threshold (≤5)");
    }

    private void SubmitSightingWithSpecies(string speciesName)
    {
        _tempImagePath = Path.Combine(Path.GetTempPath(), $"csp141_{Guid.NewGuid():N}.jpg");
        using (var image = new Image<Rgba32>(1280, 960, new Rgba32(128, 128, 128, 255)))
        using (var fs = File.Create(_tempImagePath))
        {
            image.SaveAsJpeg(fs);
        }

        _sightingsDriver.NavigateToSightingsUpload();
        _sightingsDriver.SetImageForUpload(_tempImagePath);
        _sightingsDriver.SetSpeciesName(speciesName);
        _sightingsDriver.SetLatitude(45.0);
        _sightingsDriver.SetLongitude(-123.0);
        _sightingsDriver.SetTimestamp(DateTimeOffset.Now.AddMinutes(-1));
        _sightingsDriver.SetDescription("CSP-141 acceptance test sighting.");
        _sightingsDriver.SubmitSightingsForm();
    }
}
