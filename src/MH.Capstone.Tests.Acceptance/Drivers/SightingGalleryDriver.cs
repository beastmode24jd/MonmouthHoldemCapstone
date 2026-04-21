using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Helpers;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class SightingGalleryDriver
{
    private readonly IWebDriver _webDriver;
    private readonly string _baseUrl;
    private readonly DashboardDriver _dashboardDriver;

    public SightingGalleryDriver(IWebDriver webDriver, AcceptanceTestSettings settings, DashboardDriver dashboardDriver)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _dashboardDriver = dashboardDriver;
    }

    public void NavigateToGallery()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Gallery");
        _webDriver.WaitForDocumentReady(TimeSpan.FromSeconds(10));
    }

    public bool IsOnGalleryPage()
    {
        try
        {
            return _webDriver.WaitUntil(d =>
                d.Url.Contains("/Sighting/Gallery", StringComparison.InvariantCultureIgnoreCase),
                TimeSpan.FromSeconds(3));
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
        _webDriver.WaitUntil(_ =>
        {
            var btn = _webDriver.FindElement(By.Id("filterAll"));
            return btn.GetAttribute("class")?.Contains("btn-primary") == true
                   && !btn.GetAttribute("class")!.Contains("btn-outline-primary");
        }, TimeSpan.FromSeconds(3));
    }

    public void ClickMyFilter()
    {
        var page = new SightingGalleryPageObject(_webDriver);
        page.FilterMineBtn.Click();
        // Wait briefly for JS filter to apply
        _webDriver.WaitUntil(_ =>
        {
            var btn = _webDriver.FindElement(By.Id("filterMine"));
            return btn.GetAttribute("class")?.Contains("btn-primary") == true
                   && !btn.GetAttribute("class")!.Contains("btn-outline-primary");
        }, TimeSpan.FromSeconds(3));
    }

    public bool IsAllSightingsFilterActive()
    {
        try
        {
            var btn = _webDriver.WaitForElement(By.Id("filterAll"), TimeSpan.FromSeconds(3));
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
            var btn = _webDriver.WaitForElement(By.Id("filterMine"), TimeSpan.FromSeconds(3));
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
