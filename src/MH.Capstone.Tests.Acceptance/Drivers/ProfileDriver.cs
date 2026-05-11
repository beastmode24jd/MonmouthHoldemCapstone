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
