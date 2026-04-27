using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class NotificationPreferencesDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _preferencesUrl;

    public NotificationPreferencesDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _wait = wait;
        _preferencesUrl = $"{settings.BaseUrl.TrimEnd('/')}/dashboard/notification-preferences";
    }

    public void NavigateToNotificationPreferences()
    {
        _webDriver.Navigate().GoToUrl(_preferencesUrl);
    }

    public bool IsOnNotificationPreferencesPage()
        => _webDriver.Url.Contains("notification-preferences", StringComparison.OrdinalIgnoreCase);

    public bool PreferencesFormIsVisible()
        => _webDriver.FindElements(By.Id("notificationPreferencesForm")).Count > 0;

    public bool SystemCriticalTypeIsNotVisible()
    {
        var options = _webDriver.FindElements(By.CssSelector("select option"));
        return !_webDriver.PageSource.Contains("SystemCritical", StringComparison.OrdinalIgnoreCase);
    }

    public void SetDeliveryChannel(string notificationTypeLabel, string channelLabel)
    {
        var rows = _webDriver.FindElements(By.CssSelector("table tbody tr"));
        foreach (var row in rows)
        {
            var label = row.FindElements(By.CssSelector("td:first-child")).FirstOrDefault()?.Text ?? string.Empty;
            if (!label.Contains(notificationTypeLabel, StringComparison.OrdinalIgnoreCase))
                continue;

            var select = new SelectElement(row.FindElement(By.CssSelector("select")));
            select.SelectByText(channelLabel);
            return;
        }

        throw new InvalidOperationException($"Could not find row for notification type '{notificationTypeLabel}'");
    }

    public void ClickSave()
    {
        _webDriver.FindElement(By.Id("saveNotificationPreferencesBtn")).Click();
    }

    public bool SuccessBannerIsVisible()
    {
        try
        {
            _wait.Until(d => d.FindElements(By.Id("notificationPreferenceSuccess")).Count > 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetSelectedChannel(string notificationTypeLabel)
    {
        var rows = _webDriver.FindElements(By.CssSelector("table tbody tr"));
        foreach (var row in rows)
        {
            var label = row.FindElements(By.CssSelector("td:first-child")).FirstOrDefault()?.Text ?? string.Empty;
            if (!label.Contains(notificationTypeLabel, StringComparison.OrdinalIgnoreCase))
                continue;

            var select = new SelectElement(row.FindElement(By.CssSelector("select")));
            return select.SelectedOption.Text;
        }

        throw new InvalidOperationException($"Could not find row for notification type '{notificationTypeLabel}'");
    }
}
