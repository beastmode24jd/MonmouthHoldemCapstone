using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class SightingsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public SightingsDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToSightingsUpload()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Create");
        try
        {
            _wait.Until(d =>
            {
                try
                {
                    var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                    return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            });
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>Sets the image file input to the given path without submitting the form.</summary>
    public void SetImageForUpload(string absoluteFilePath)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        // ensure the input exists before sending keys
        var input = _wait.Until(d => d.FindElement(By.CssSelector("input[type='file']")));
        input.SendKeys(absoluteFilePath);
    }

    /// <summary>Sets the image file input and then submits the form.</summary>
    public void UploadFileAndSubmit(string absoluteFilePath)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        var input = _wait.Until(d => d.FindElement(By.CssSelector("input[type='file']")));
        input.SendKeys(absoluteFilePath);
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].click();", page.SubmitBtn);
    }

    /// <summary>Sets the latitude input to the given value.</summary>
    public void SetLatitude(double latitude)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        var input = _wait.Until(d => d.FindElement(By.CssSelector("input[name='Latitude']")));
        input.Clear();
        input.SendKeys(latitude.ToString());
    }

    /// <summary>Sets the longitude input to the given value.</summary>
    public void SetLongitude(double longitude)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        var input = _wait.Until(d => d.FindElement(By.CssSelector("input[name='Longitude']")));
        input.Clear();
        input.SendKeys(longitude.ToString());
    }

    /// <summary>Sets the timestamp input to the given value.</summary>
    public void SetTimestamp(DateTimeOffset timestamp)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        // Use a short wait to ensure the script is executed against an available element.
        new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(d =>
        {
            try
            {
                ((IJavaScriptExecutor)d).ExecuteScript(
                    "arguments[0].value = arguments[1];",
                    page.TimeInput,
                    timestamp.ToString("yyyy-MM-ddTHH:mm"));
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>Sets the description input to the given value.</summary>
    public void SetDescription(string description)
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        var input = _wait.Until(d =>
            d.FindElement(By.CssSelector("textarea[name='Description'], input[name='Description']")));
        input.Clear();
        input.SendKeys(description);
    }

    /// <summary>Clicks the submit button on the currently displayed upload form.</summary>
    public void SubmitSightingsForm()
    {
        var page = new SightingsUploadPageObject(_webDriver, _baseUrl);
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].click();", page.SubmitBtn);
        _wait.Until(d => !IsOnSightingsUploadPage()
            || HasVisibleValidationErrors());
    }

    /// <summary>
    /// Returns true if the browser is still on either of the sightings upload URLs
    /// (/Sighting/Create or /Sighting/Upload), which indicates that the form
    /// submission was rejected rather than redirected to the dashboard.
    /// </summary>
    public bool IsOnSightingsUploadPage()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(d =>
                d.Url.Contains("/Sighting/Upload", StringComparison.InvariantCultureIgnoreCase) ||
                d.Url.Contains("/Sighting/Create", StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true when at least one ASP.NET model-validation error span is both
    /// visible and contains text.  ASP.NET MVC adds the <c>field-validation-error</c>
    /// CSS class to <c>asp-validation-for</c> spans when the ModelState has errors.
    /// </summary>
    public bool HasVisibleValidationErrors()
    {
        try
        {
            var result = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(1)).Until(d =>
            {
                var elements = d.FindElements(By.CssSelector(".field-validation-error"));
                return elements.Any(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
            });

            if (result)
                return true;
        }
        catch
        {
            // timed out waiting for visible errors; fall through to logging below
        }

        var errorSpans = _webDriver.FindElements(By.CssSelector(".field-validation-error"));

        TestContext.Out.WriteLine($"[{nameof(HasVisibleValidationErrors)}] Span count: {errorSpans.Count}");
        foreach (var span in errorSpans)
        {
            TestContext.Out.WriteLine($"[{nameof(HasVisibleValidationErrors)}] Span text: '{span.Text}', displayed: {span.Displayed}");
        }

        return false;
    }
}
