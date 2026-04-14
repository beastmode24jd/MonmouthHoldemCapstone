using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class SightingsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly string _baseUrl;

    public SightingsDriver(IWebDriver webDriver, AcceptanceTestSettings settings)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    public void NavigateToSightingsUpload() =>
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Create");

    /// <summary>Sets the image file input to the given path without submitting the form.</summary>
    public void SetImageForUpload(string absoluteFilePath)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        page.ImageUploadBtn.SendKeys(absoluteFilePath);
    }

    /// <summary>Sets the image file input and then submits the form.</summary>
    public void UploadFileAndSubmit(string absoluteFilePath)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        page.ImageUploadBtn.SendKeys(absoluteFilePath);
        page.SubmitBtn.Click();
    }

    /// <summary>Sets the latitude input to the given value.</summary>
    public void SetLatitude(double latitude)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        page.LatInput.Clear();
        page.LatInput.SendKeys(latitude.ToString());
    }

    /// <summary>Sets the longitude input to the given value.</summary>
    public void SetLongitude(double longitude)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        page.LongInput.Clear();
        page.LongInput.SendKeys(longitude.ToString());
    }

    /// <summary>Sets the timestamp input to the given value.</summary>
    public void SetTimestamp(DateTimeOffset timestamp)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        page.TimeInput.Clear();
        page.TimeInput.SendKeys(timestamp.ToString("yyyy-MM-ddTHH:mm"));
    }

    /// <summary>Sets the description input to the given value.</summary>
    public void SetDescription(string description)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        page.DescInput.Clear();
        page.DescInput.SendKeys(description);
    }

    /// <summary>Clicks the submit button on the currently displayed upload form.</summary>
    public void SubmitSightingsForm()
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        page.SubmitBtn.Click();
    }

    /// <summary>
    /// Returns true if the browser is still on either of the sightings upload URLs
    /// (/Sighting/Create or /Sighting/Upload), which indicates that the form
    /// submission was rejected rather than redirected to the dashboard.
    /// </summary>
    public bool IsOnSightingsUploadPage() =>
        _webDriver.Url.Contains("/Sighting/Upload", StringComparison.InvariantCultureIgnoreCase) ||
        _webDriver.Url.Contains("/Sighting/Create", StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Returns true when at least one ASP.NET model-validation error span is both
    /// visible and contains text.  ASP.NET MVC adds the <c>field-validation-error</c>
    /// CSS class to <c>asp-validation-for</c> spans when the ModelState has errors.
    /// </summary>
    public bool HasVisibleValidationErrors() =>
        _webDriver
            .FindElements(By.CssSelector(".field-validation-error"))
            .Any(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
}
