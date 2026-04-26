using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class SightingGalleryDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly DashboardDriver _dashboardDriver;

    public SightingGalleryDriver(IWebDriver webDriver, AcceptanceTestSettings settings,
        DashboardDriver dashboardDriver, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _dashboardDriver = dashboardDriver;
        _wait = wait;
    }

    public void NavigateToGallery()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Gallery");
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

    public bool IsOnGalleryPage()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(d =>
                d.Url.Contains("/Sighting/Gallery", StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public void ClickAllSightingsFilter()
    {
        var page = new SightingGalleryPageObject(_webDriver);
        page.FilterAllBtn.Click();
        // Wait briefly for JS filter to apply
        new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(_ =>
        {
            var btn = _webDriver.FindElement(By.Id("filterAll"));
            return btn.GetAttribute("class")?.Contains("btn-primary") == true
                   && !btn.GetAttribute("class")!.Contains("btn-outline-primary");
        });
    }

    public void ClickMyFilter()
    {
        var page = new SightingGalleryPageObject(_webDriver);
        page.FilterMineBtn.Click();
        // Wait briefly for JS filter to apply
        new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(_ =>
        {
            var btn = _webDriver.FindElement(By.Id("filterMine"));
            return btn.GetAttribute("class")?.Contains("btn-primary") == true
                   && !btn.GetAttribute("class")!.Contains("btn-outline-primary");
        });
    }

    public bool IsAllSightingsFilterActive()
    {
        try
        {
            var btn = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(d =>
                d.FindElement(By.Id("filterAll")));
            var cls = btn.GetAttribute("class") ?? string.Empty;
            return cls.Contains("btn-primary") && !cls.Contains("btn-outline-primary");
        }
        catch
        {
            return false;
        }
    }

    public bool IsMyFilterActive()
    {
        try
        {
            var btn = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(d =>
                d.FindElement(By.Id("filterMine")));
            var cls = btn.GetAttribute("class") ?? string.Empty;
            return cls.Contains("btn-primary") && !cls.Contains("btn-outline-primary");
        }
        catch
        {
            return false;
        }
    }

    /// Returns the number of sighting cards currently visible in the grid.
    public int GetVisibleSightingCount()
    {
        var cards = _webDriver.FindElements(By.CssSelector(".sighting-card-wrapper"));
        return cards.Count(c => c.Displayed);
    }

    /// Returns distinct set of usernames shown in attribution labels on visible cards.
    public ISet<string> GetVisibleAttributionUsernames()
    {
        var cards = _webDriver.FindElements(By.CssSelector(".sighting-card-wrapper"));
        var visibleCards = cards.Where(c => c.Displayed);
        var names = new HashSet<string>();
        foreach (var card in visibleCards)
        {
            var attr = card.FindElements(By.CssSelector(".sighting-attribution"));
            foreach (var a in attr)
            {
                if (!string.IsNullOrWhiteSpace(a.Text))
                    names.Add(a.Text.Trim());
            }
        }
        return names;
    }

    public bool IsMyEmptyStateVisible()
    {
        var els = _webDriver.FindElements(By.Id("emptyStateMine"));
        return els.Count > 0 && els[0].Displayed;
    }

    public bool HasUploadLink()
    {
        var els = _webDriver.FindElements(By.Id("emptyStateMine"));
        if (els.Count == 0) return false;
        var links = els[0].FindElements(By.TagName("a"));
        return links.Any(l => l.Displayed &&
            l.GetAttribute("href")?.Contains("/Sighting/", StringComparison.OrdinalIgnoreCase) == true);
    }

    public void NavigateAwayAndReturn()
    {
        _dashboardDriver.NavigateToDashboard();
        NavigateToGallery();
    }
}
