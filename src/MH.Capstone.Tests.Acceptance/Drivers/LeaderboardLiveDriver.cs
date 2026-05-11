using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// CSP-180: Drives the live leaderboard view. Owns an optional secondary
/// ChromeDriver so multi-client scenarios (one user scoring, another observing)
/// can be exercised without modifying the shared TestWebAppHost driver.
/// </summary>
[ExcludeFromCodeCoverage]
public class LeaderboardLiveDriver : IDisposable
{
    private readonly IWebDriver _primaryDriver;
    private readonly AcceptanceTestSettings _settings;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    private IWebDriver? _secondDriver;

    public LeaderboardLiveDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _primaryDriver = webDriver;
        _settings = settings;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    // ----- Primary-driver operations -----

    public void NavigateToLeaderboard()
    {
        _primaryDriver.Navigate().GoToUrl($"{_baseUrl}/Leaderboard");
        WaitForPageReady(_primaryDriver);
    }

    public string GetPointsForUserOnPrimary(string userId)
    {
        var page = new LeaderboardPageObject(_primaryDriver);
        return page.PointsTextFor(userId);
    }

    public bool LiveNotificationToastVisibleOnPrimary()
    {
        var page = new LeaderboardPageObject(_primaryDriver);
        return page.LiveNotificationToasts.Any(t => t.Displayed);
    }

    public void TriggerReconnectOnPrimary()
    {
        var js = (IJavaScriptExecutor)_primaryDriver;
        js.ExecuteScript("window.leaderboardLive && window.leaderboardLive.reconnect && window.leaderboardLive.reconnect();");
    }

    // ----- Second-driver operations -----

    public IWebDriver OpenSecondClient()
    {
        if (_secondDriver != null) return _secondDriver;

        var options = new ChromeOptions();
        if (_settings.HeadlessSelenium)
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1920,1080");
        }
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--allow-insecure-localhost");

        _secondDriver = new ChromeDriver(options);
        _secondDriver.Navigate().GoToUrl(_baseUrl);
        return _secondDriver;
    }

    public void NavigateSecondClientToLeaderboard()
    {
        var driver = OpenSecondClient();
        driver.Navigate().GoToUrl($"{_baseUrl}/Leaderboard");
        WaitForPageReady(driver);
    }

    public void LoginSecondClientAs(string email, string password)
    {
        var driver = OpenSecondClient();
        driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");
        WaitForPageReady(driver);

        driver.FindElement(By.Id("emailField")).SendKeys(email);
        driver.FindElement(By.Id("passwordField")).SendKeys(password);
        driver.FindElement(By.Id("submitBtn")).Click();

        new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(d =>
            !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
    }

    public string GetPointsForUserOnSecondClient(string userId)
    {
        var driver = OpenSecondClient();
        var page = new LeaderboardPageObject(driver);
        return page.PointsTextFor(userId);
    }

    public bool WaitForPointsChangeOnSecondClient(string userId, string previousValue, TimeSpan timeout)
    {
        var driver = OpenSecondClient();
        try
        {
            return new WebDriverWait(driver, timeout).Until(d =>
            {
                var page = new LeaderboardPageObject(d);
                var rows = page.RowsFor(userId);
                if (rows.Count == 0) return false;
                var current = page.PointsTextFor(userId);
                return !string.Equals(current, previousValue, StringComparison.Ordinal);
            });
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public bool LiveNotificationToastVisibleOnSecondClient()
    {
        var driver = OpenSecondClient();
        var page = new LeaderboardPageObject(driver);
        return page.LiveNotificationToasts.Any(t => t.Displayed);
    }

    // ----- Cleanup -----

    public void Dispose()
    {
        if (_secondDriver != null)
        {
            try { _secondDriver.Quit(); } catch { /* ignore */ }
            try { _secondDriver.Dispose(); } catch { /* ignore */ }
            _secondDriver = null;
        }
    }

    private static void WaitForPageReady(IWebDriver driver)
    {
        new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(d =>
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
