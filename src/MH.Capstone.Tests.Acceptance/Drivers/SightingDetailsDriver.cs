using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class SightingDetailsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public SightingDetailsDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToDetails(Guid sightingId)
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Details/{sightingId}");
        WaitForPageReady();
    }

    public void ClickFirstSightingCardLink()
    {
        var link = _wait.Until(d =>
        {
            var elements = d.FindElements(By.CssSelector(".sighting-card-link"));
            return elements.Count > 0 ? elements[0] : null;
        });
        link!.Click();
        WaitForPageReady();
    }

    public bool IsOnDetailsPage()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
                d.Url.Contains("/Sighting/Details/", StringComparison.InvariantCultureIgnoreCase)
                && d.FindElements(By.Id("sightingDetailsContainer")).Count > 0);
        }
        catch
        {
            return false;
        }
    }

    public bool IsOnNotFoundPage()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
                d.FindElements(By.Id("sightingNotFoundMessage")).Count > 0);
        }
        catch
        {
            return false;
        }
    }

    private void WaitForPageReady()
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
}
