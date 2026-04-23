using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class NotificationsPageObject
{
    private readonly IWebDriver _webDriver;

    public NotificationsPageObject(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }

    public IWebElement? MarkAllReadForm
        => _webDriver.FindElements(By.Id("markAllReadForm")).FirstOrDefault();

    public IWebElement? MarkAllReadBtn
        => _webDriver.FindElements(By.Id("markAllReadBtn")).FirstOrDefault();

    public IWebElement? DeleteAllForm
        => _webDriver.FindElements(By.Id("deleteAllForm")).FirstOrDefault();

    public IWebElement? DeleteAllBtn
        => _webDriver.FindElements(By.Id("deleteAllBtn")).FirstOrDefault();

    public IWebElement? EmptyState
        => _webDriver.FindElements(By.Id("notificationsEmptyState")).FirstOrDefault();

    public IReadOnlyCollection<IWebElement> UnreadRows
        => _webDriver.FindElements(By.CssSelector(".notification-row.unread"));

    public IReadOnlyCollection<IWebElement> AllRows
        => _webDriver.FindElements(By.CssSelector(".notification-row"));
}
