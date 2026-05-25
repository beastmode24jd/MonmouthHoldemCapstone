using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

// CSP-187: Selenium driver for /account/{guid} — Follow/Unfollow + Block/Unblock toggles.
[ExcludeFromCodeCoverage]
public class ProfileDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public ProfileDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToProfile(Guid userId)
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/account/{userId}");
        WaitForReady();
    }

    // CSP-205: viewing one's own profile uses /account with no id segment.
    public void NavigateToOwnProfile()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/account");
        WaitForReady();
    }

    private void WaitForReady()
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

    // CSP-205: profile-content read helpers.
    public bool IsPointsValueVisible()
        => _webDriver.FindElements(By.Id("profilePointsValue")).Any(e => e.Displayed);

    public int GetPointsValue()
    {
        var el = _wait.Until(d => d.FindElement(By.Id("profilePointsValue")));
        return int.Parse(el.Text.Trim());
    }

    public bool IsRecentClubsSectionPresent()
        => _webDriver.FindElements(By.Id("profileRecentClubs")).Count > 0;

    // CSP-215: Recent Sightings now renders via the shared _SightingCardGrid partial,
    // which emits .sighting-card-wrapper cards inside the #profileRecentSightings container.
    // Scope the card selector to that container so cards from any other grid on the page
    // (e.g. shared layout fragments) can't inflate the count.
    public bool IsRecentSightingsSectionPresent()
        => _webDriver.FindElements(By.Id("profileRecentSightings")).Count > 0;

    public int GetRecentSightingsCardCount()
        => _webDriver.FindElements(By.CssSelector("#profileRecentSightings .sighting-card-wrapper")).Count;

    public string GetBioText()
    {
        var el = _wait.Until(d => d.FindElement(By.Id("profileBio")));
        return el.Text.Trim();
    }

    public bool IsFollowButtonVisible()
        => _webDriver.FindElements(By.Id("followButton")).Any(e => e.Displayed);

    public bool IsUnfollowButtonVisible()
        => _webDriver.FindElements(By.Id("unfollowButton")).Any(e => e.Displayed);

    public bool IsBlockButtonVisible()
        => _webDriver.FindElements(By.Id("blockButton")).Any(e => e.Displayed);

    public bool IsUnblockButtonVisible()
        => _webDriver.FindElements(By.Id("unblockButton")).Any(e => e.Displayed);

    public void ClickFollow() => ClickAndWaitForReload("followButton");
    public void ClickUnfollow() => ClickAndWaitForReload("unfollowButton");
    public void ClickBlock() => ClickAndWaitForReload("blockButton");
    public void ClickUnblock() => ClickAndWaitForReload("unblockButton");

    private void ClickAndWaitForReload(string buttonId)
    {
        var btn = _wait.Until(d => d.FindElement(By.Id(buttonId)));
        btn.Click();
        // Wait for the clicked element to go stale — that signals the page has actually
        // reloaded after the form's redirect, so subsequent navigations don't race the
        // in-flight redirect. readyState alone is not enough: it stays "complete" on the
        // current page until the new doc starts loading.
        _wait.Until(d =>
        {
            try { _ = btn.TagName; return false; }
            catch (StaleElementReferenceException) { return true; }
            catch { return true; }
        });
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
