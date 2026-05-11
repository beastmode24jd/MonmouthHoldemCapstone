using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

// CSP-187: Selenium driver for the /Feed activity feed.
[ExcludeFromCodeCoverage]
public class FeedDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public FeedDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToFeed()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Feed");
        // Wait until the browser actually lands on /Feed AND the page contains the
        // Feed view's container OR the no-followees empty state. Without the URL
        // check, GoToUrl can race with an in-flight redirect from a prior form submit
        // and the readyState check passes on the wrong page.
        _wait.Until(d =>
        {
            try
            {
                var url = d.Url ?? string.Empty;
                if (!url.Contains("/Feed", StringComparison.OrdinalIgnoreCase))
                    return false;
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                if (!string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase))
                    return false;
                var marker = d.FindElements(By.Id("feedSightingsGrid")).Count
                    + d.FindElements(By.Id("feedNoFollowees")).Count
                    + d.FindElements(By.Id("feedEmptyFollowees")).Count;
                return marker > 0;
            }
            catch { return false; }
        });
    }

    public int GetVisibleCardCount()
        => _webDriver.FindElements(By.CssSelector(".feed-card-wrapper")).Count(e => e.Displayed);

    public IReadOnlyList<string> GetVisibleAuthorIds()
        => _webDriver.FindElements(By.CssSelector(".feed-card-wrapper"))
            .Where(e => e.Displayed)
            .Select(e => e.GetAttribute("data-user-id") ?? string.Empty)
            .ToList();

    public bool IsNoFolloweesEmptyStateVisible()
    {
        var els = _webDriver.FindElements(By.Id("feedNoFollowees"));
        return els.Count > 0 && els[0].Displayed;
    }
}
