using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;

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
