// CSP-193: Auto-populate latitude/longitude on /Sighting/Create via Geolocation API.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "csp193")]
[ExcludeFromCodeCoverage]
public class CSP193StepDefinitions
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly SightingsDriver _sightingsDriver;

    private string? _generatedImagePath;

    public CSP193StepDefinitions(IWebDriver webDriver, WebDriverWait wait, SightingsDriver sightingsDriver)
    {
        _webDriver = webDriver;
        _wait = wait;
        _sightingsDriver = sightingsDriver;
    }

    [AfterScenario("csp193")]
    public void CleanupGeneratedImage()
    {
        if (_generatedImagePath is not null && File.Exists(_generatedImagePath))
            File.Delete(_generatedImagePath);
    }

    private ChromeDriver Chrome =>
        ((_webDriver as IWrapsDriver)?.WrappedDriver as ChromeDriver)
        ?? throw new InvalidOperationException("CSP-193 BDD requires the underlying driver to be ChromeDriver for CDP geolocation control.");

    // CDP overrides leak across scenarios on the same browser instance, so reset before each.
    [BeforeScenario("csp193")]
    public void ResetGeolocationOverride()
    {
        try { Chrome.ExecuteCdpCommand("Emulation.clearGeolocationOverride", new Dictionary<string, object?>()); } catch { /* nothing to clear */ }
        try { Chrome.ExecuteCdpCommand("Browser.resetPermissions", new Dictionary<string, object?>()); } catch { /* nothing to reset */ }
    }

    [Given("the browser grants geolocation with latitude (.*) and longitude (.*)")]
    public void GivenBrowserGrantsGeolocation(double latitude, double longitude)
    {
        Chrome.ExecuteCdpCommand("Browser.grantPermissions", new Dictionary<string, object?>
        {
            ["permissions"] = new[] { "geolocation" },
        });
        Chrome.ExecuteCdpCommand("Emulation.setGeolocationOverride", new Dictionary<string, object?>
        {
            ["latitude"]  = latitude,
            ["longitude"] = longitude,
            ["accuracy"]  = 1,
        });
    }

    [Given("the browser denies geolocation permission")]
    public void GivenBrowserDeniesGeolocation()
    {
        // Browser.setPermission with setting=denied is the most reliable way to fire the
        // PERMISSION_DENIED callback without leaving the browser waiting on a real prompt.
        Chrome.ExecuteCdpCommand("Browser.setPermission", new Dictionary<string, object?>
        {
            ["permission"] = new Dictionary<string, object?> { ["name"] = "geolocation" },
            ["setting"]    = "denied",
        });
    }

    [When("Alex navigates to the sighting upload page")]
    public void WhenAlexNavigatesToSightingUploadPage()
    {
        _sightingsDriver.NavigateToSightingsUpload();
    }

    [When("Alex submits the sightings form")]
    public void WhenAlexSubmitsSightingsForm()
    {
        _sightingsDriver.SubmitSightingsForm();
    }

    [Then("the latitude input should display {string}")]
    public void ThenLatitudeInputShouldDisplay(string expected)
    {
        var input = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("latitudeInput"));
            return string.IsNullOrEmpty(el.GetAttribute("value")) ? null : el;
        });
        input!.GetAttribute("value").Should().Be(expected,
            "the geolocation override should populate the latitude input on page load");
    }

    [Then("the longitude input should display {string}")]
    public void ThenLongitudeInputShouldDisplay(string expected)
    {
        var input = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("longitudeInput"));
            return string.IsNullOrEmpty(el.GetAttribute("value")) ? null : el;
        });
        input!.GetAttribute("value").Should().Be(expected,
            "the geolocation override should populate the longitude input on page load");
    }

    [Then("the location status message should be visible")]
    public void ThenLocationStatusShouldBeVisible()
    {
        var status = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("locationStatus"));
            return el.Displayed && !string.IsNullOrWhiteSpace(el.Text) ? el : null;
        });
        status.Should().NotBeNull("a denied permission must show an inline message near the coordinate inputs");
    }

    [Then("the location status message should mention entering coordinates manually")]
    public void ThenLocationStatusShouldMentionManually()
    {
        var status = _webDriver.FindElement(By.Id("locationStatus"));
        status.Text.ToLowerInvariant().Should().Contain("manually",
            "the denied-permission message must direct the user to enter coordinates manually");
    }

    [Then("the latitude input should be empty or zero")]
    public void ThenLatitudeInputShouldBeEmptyOrZero()
    {
        var value = (_webDriver.FindElement(By.Id("latitudeInput")).GetAttribute("value") ?? string.Empty).Trim();
        (string.IsNullOrEmpty(value) || decimal.TryParse(value, out var d) && d == 0m)
            .Should().BeTrue($"latitude must remain editable and not auto-filled; was '{value}'");
    }

    [Then("the longitude input should be empty or zero")]
    public void ThenLongitudeInputShouldBeEmptyOrZero()
    {
        var value = (_webDriver.FindElement(By.Id("longitudeInput")).GetAttribute("value") ?? string.Empty).Trim();
        (string.IsNullOrEmpty(value) || decimal.TryParse(value, out var d) && d == 0m)
            .Should().BeTrue($"longitude must remain editable and not auto-filled; was '{value}'");
    }

    [Then("a validation error should be shown about coordinates not being zero")]
    public void ThenValidationErrorAboutZeroCoordinates()
    {
        // The page may render the model-level error via the validation summary or a span.
        var pageText = _webDriver.PageSource.ToLowerInvariant();
        pageText.Should().Match(p => p.Contains("cannot both be 0") || p.Contains("cannot both be zero")
                                  || p.Contains("enter valid coordinates"),
            "submitting with default 0,0 must surface NotDefaultCoordinatesAttribute's error message");
    }
}
