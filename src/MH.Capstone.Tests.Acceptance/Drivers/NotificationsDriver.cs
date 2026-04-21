using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Helpers;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class NotificationsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly string _baseUrl;

    public NotificationsDriver(IWebDriver webDriver, AcceptanceTestSettings settings)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    public void NavigateToNotifications()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/notifications");
        _webDriver.WaitForDocumentReady(TimeSpan.FromSeconds(10));
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
        _webDriver.WaitUntil(d =>
            d.FindElements(By.CssSelector(".notification-row.unread")).Count == 0,
            TimeSpan.FromSeconds(10));
    }

    public void ClickDeleteAll()
    {
        var page = new NotificationsPageObject(_webDriver);
        page.DeleteAllBtn?.Click();

        // Delete All triggers a page reload; wait for document ready
        _webDriver.WaitForDocumentReady(TimeSpan.FromSeconds(10));
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
            return _webDriver.WaitUntil(d =>
            {
                var badge = d.FindElements(By.Id("pendingNotifBadge")).FirstOrDefault();
                return badge == null || !badge.Displayed;
            }, TimeSpan.FromSeconds(5));
        }
        catch
        {
            return false;
        }
    }
}
