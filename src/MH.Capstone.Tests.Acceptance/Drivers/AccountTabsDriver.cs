using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// CSP-211: Drives the follower/following tab UI on <c>/account/{id}</c>.
/// Tab buttons themselves carry the count text, so this driver exposes both
/// "read the count" and "click the count to open the list" operations.
/// </summary>
[ExcludeFromCodeCoverage]
public class AccountTabsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public AccountTabsDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToOwnAccount()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/account");
        WaitForGraphSection();
    }

    public void NavigateToAccount(Guid userId)
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/account/{userId}");
        WaitForGraphSection();
    }

    private void WaitForGraphSection()
    {
        _wait.Until(d => d.FindElements(By.Id("followGraphSection")).Count > 0);
    }

    public int GetFollowerCount() => ReadIntFromSpan("followerCount");
    public int GetFollowingCount() => ReadIntFromSpan("followingCount");

    public void ClickFollowerCount() => _webDriver.FindElement(By.Id("followersTabBtn")).Click();
    public void ClickFollowingCount() => _webDriver.FindElement(By.Id("followingTabBtn")).Click();

    /// <summary>Returns the display names rendered in the active follower-list rows.</summary>
    public IReadOnlyList<string> GetFollowerDisplayNames()
        => GetDisplayNamesInside("followersTab");

    public IReadOnlyList<string> GetFollowingDisplayNames()
        => GetDisplayNamesInside("followingTab");

    public bool IsFollowersEmptyStateVisible()
        => _webDriver.FindElements(By.Id("followersEmptyState")).Count > 0;

    public bool IsFollowingEmptyStateVisible()
        => _webDriver.FindElements(By.Id("followingEmptyState")).Count > 0;

    public bool DoesFollowerRowLinkToProfile(string displayName, Guid expectedUserId)
        => RowLinksTo("followersTab", displayName, expectedUserId);

    public bool DoesFollowerRowShowAvatar(string displayName)
        => RowShowsAvatar("followersTab", displayName);

    private int ReadIntFromSpan(string elementId)
    {
        var raw = _webDriver.FindElement(By.Id(elementId)).Text.Trim();
        // Convert.ToInt32 — defensive even though the chip text is always a clean integer.
        return Convert.ToInt32(raw);
    }

    private IReadOnlyList<string> GetDisplayNamesInside(string tabPaneId)
    {
        var pane = _webDriver.FindElement(By.Id(tabPaneId));
        return pane.FindElements(By.CssSelector(".follow-list-display-name"))
            .Select(e => e.Text.Trim())
            .ToList();
    }

    private bool RowLinksTo(string tabPaneId, string displayName, Guid expectedUserId)
    {
        var pane = _webDriver.FindElement(By.Id(tabPaneId));
        foreach (var item in pane.FindElements(By.CssSelector(".follow-list-item")))
        {
            var name = item.FindElement(By.CssSelector(".follow-list-display-name")).Text.Trim();
            if (!string.Equals(name, displayName, StringComparison.Ordinal)) continue;
            var href = item.FindElement(By.CssSelector("a")).GetAttribute("href") ?? string.Empty;
            return href.Contains(expectedUserId.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private bool RowShowsAvatar(string tabPaneId, string displayName)
    {
        var pane = _webDriver.FindElement(By.Id(tabPaneId));
        foreach (var item in pane.FindElements(By.CssSelector(".follow-list-item")))
        {
            var name = item.FindElement(By.CssSelector(".follow-list-display-name")).Text.Trim();
            if (!string.Equals(name, displayName, StringComparison.Ordinal)) continue;
            return item.FindElements(By.CssSelector("img.follow-list-avatar")).Count > 0;
        }
        return false;
    }
}
