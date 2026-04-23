using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class NotificationsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public NotificationsDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToNotifications()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/notifications");
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

    public bool IsMarkAllReadVisible()
    {
        var page = new NotificationsPageObject(_webDriver);
        var form = page.MarkAllReadForm;
        return form != null && !(form.GetAttribute("class") ?? string.Empty).Contains("d-none") && form.Displayed;
    }

    public bool IsDeleteAllVisible()
    {
        var page = new NotificationsPageObject(_webDriver);
        var form = page.DeleteAllForm;
        return form != null && !(form.GetAttribute("class") ?? string.Empty).Contains("d-none") && form.Displayed;
    }

    public bool HasUnreadNotifications()
    {
        var page = new NotificationsPageObject(_webDriver);
        return page.UnreadRows.Count > 0;
    }

    public bool HasAnyNotifications()
    {
        var page = new NotificationsPageObject(_webDriver);
        return page.AllRows.Count > 0;
    }

    public void ClickMarkAllRead()
    {
        var page = new NotificationsPageObject(_webDriver);
        page.MarkAllReadBtn?.Click();

        // Wait until there are no more unread rows
        new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10)).Until(d =>
            d.FindElements(By.CssSelector(".notification-row.unread")).Count == 0);
    }

    public void ClickDeleteAll()
    {
        var page = new NotificationsPageObject(_webDriver);
        var btn = page.DeleteAllBtn;
        btn?.Click();

        // The JS handler makes an async DELETE fetch, then calls location.reload().
        // Checking readyState immediately would return "complete" on the current page
        // before the reload starts.  Wait for the button element to go stale first
        // (staleness signals that location.reload() has begun navigating away), then
        // wait for the reloaded page to finish loading.
        if (btn != null)
        {
            _wait.Until(d =>
            {
                try { var tag = btn.TagName; return false; }
                catch (StaleElementReferenceException) { return true; }
            });
        }

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

    public bool IsEmptyStateVisible()
    {
        var page = new NotificationsPageObject(_webDriver);
        return page.EmptyState?.Displayed == true;
    }

    public bool IsNotificationBadgeClear()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
            {
                var badge = d.FindElements(By.Id("pendingNotifBadge")).FirstOrDefault();
                return badge == null || !badge.Displayed;
            });
        }
        catch
        {
            return false;
        }
    }
}
