using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class LiveNotificationSettingsPageObject
{
    private readonly IWebDriver _webDriver;

    public LiveNotificationSettingsPageObject(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }

    public IWebElement Toggle => _webDriver.FindElement(By.Id("liveNotificationsToggle"));
    public IWebElement SaveButton => _webDriver.FindElement(By.Id("saveLiveNotificationsBtn"));

    public IWebElement? SuccessMessage =>
        _webDriver.FindElements(By.Id("liveNotificationsSuccess")).FirstOrDefault();
}
