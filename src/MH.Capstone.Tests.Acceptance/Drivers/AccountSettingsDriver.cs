using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class AccountSettingsDriver
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _settingsUrl;
    private readonly string _dashboardUrl;

    public AccountSettingsDriver(IWebDriver driver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _driver = driver;
        _wait = wait;
        _settingsUrl = $"{settings.BaseUrl.TrimEnd('/')}/dashboard/settings";
        _dashboardUrl = $"{settings.BaseUrl.TrimEnd('/')}/dashboard";
    }

    public void NavigateToSettings()
    {
        _driver.Navigate().GoToUrl(_settingsUrl);
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

    public void NavigateToDashboard()
    {
        _driver.Navigate().GoToUrl(_dashboardUrl);
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

    public bool IsOnSettingsPage()
        => _driver.Url.Contains("/dashboard/settings", StringComparison.OrdinalIgnoreCase);

    public bool IsOnLoginPage()
        => _driver.Url.Contains("/account/login", StringComparison.OrdinalIgnoreCase);

    public void ClickAccountSettingsLink()
    {
        var link = _wait.Until(d => d.FindElement(By.Id("accountSettingsLink")));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", link);
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

    public bool DisplayNameFormIsVisible()
        => _driver.FindElements(By.Id("displayNameInput")).Count > 0
           && _driver.FindElements(By.Id("updateDisplayNameBtn")).Count > 0;

    public bool NotificationPreferencesLinkIsVisible()
        => _driver.FindElements(By.Id("notificationPreferencesLink")).Count > 0;

    public bool AccountSettingsFormsAreAbsentFromDashboard()
        => _driver.FindElements(By.Id("displayNameInput")).Count == 0
           && _driver.FindElements(By.Id("updateDisplayNameBtn")).Count == 0;

    public bool AccountSettingsLinkIsOnDashboard()
        => _driver.FindElements(By.Id("accountSettingsLink")).Count > 0;

    // CSP-205: enter `newBio` in the textarea on /dashboard/settings and submit the bio form.
    // Caller is responsible for navigating elsewhere afterward to read the updated value.
    public void UpdateBio(string newBio)
    {
        NavigateToSettings();
        var input = _wait.Until(d => d.FindElement(By.Id("bioInput")));
        input.Clear();
        input.SendKeys(newBio);

        var submit = _driver.FindElement(By.CssSelector("#bioForm button[type=submit]"));
        submit.Click();

        // Form submit posts to Dashboard/UpdateUserBio and redirects back to /dashboard/settings.
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
