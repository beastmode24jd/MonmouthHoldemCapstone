using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// CSP-176: Selenium driver for the Leaderboard page (/Leaderboard).
/// </summary>
[ExcludeFromCodeCoverage]
public class LeaderboardDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public LeaderboardDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToLeaderboard()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Leaderboard");
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

    public IWebElement GetLeaderboardTable()
        => _webDriver.FindElement(By.CssSelector("table.table"));

    public bool TableIsInsideResponsiveWrapper()
    {
        var hit = ((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "var t = document.querySelector('table.table'); " +
            "return t ? !!t.closest('.table-responsive') : false;");
        return hit is bool b && b;
    }
}
