using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// CSP-180: Drives the per-user opt-in page for live notifications.
/// Lives at /dashboard/live-notifications (added in CSP-180).
/// </summary>
[ExcludeFromCodeCoverage]
public class LiveNotificationSettingsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public LiveNotificationSettingsDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToLiveSettings()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/dashboard/live-notifications");
        WaitForPageReady();
    }

    public bool IsToggleEnabled()
    {
        var page = new LiveNotificationSettingsPageObject(_webDriver);
        return page.Toggle.Selected;
    }

    public void DisableLiveNotifications()
    {
        NavigateToLiveSettings();
        var page = new LiveNotificationSettingsPageObject(_webDriver);
        if (page.Toggle.Selected)
        {
            page.Toggle.Click();
        }
        page.SaveButton.Click();

        _wait.Until(d =>
        {
            var msg = d.FindElements(By.Id("liveNotificationsSuccess"));
            return msg.Count > 0 && msg[0].Displayed;
        });
    }

    public void EnableLiveNotifications()
    {
        NavigateToLiveSettings();
        var page = new LiveNotificationSettingsPageObject(_webDriver);
        if (!page.Toggle.Selected)
        {
            page.Toggle.Click();
        }
        page.SaveButton.Click();

        _wait.Until(d =>
        {
            var msg = d.FindElements(By.Id("liveNotificationsSuccess"));
            return msg.Count > 0 && msg[0].Displayed;
        });
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
