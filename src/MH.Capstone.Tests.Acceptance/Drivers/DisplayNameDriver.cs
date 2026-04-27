using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class DisplayNameDriver
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public DisplayNameDriver(IWebDriver driver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _driver = driver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    /// <summary>Returns true when the Set Your Display Name page is shown.</summary>
    public bool IsSetDisplayNamePageVisible()
    {
        try
        {
            return _wait.Until(d =>
                d.FindElements(By.Id("setDisplayNameField")).Count > 0);
        }
        catch { return false; }
    }

    /// <summary>Submits the Set Your Display Name form with the given display name.</summary>
    public void SubmitSetDisplayName(string displayName)
    {
        var field = _wait.Until(d => d.FindElement(By.Id("setDisplayNameField")));
        field.Clear();
        field.SendKeys(displayName);
        _wait.Until(d => d.FindElement(By.Id("setDisplayNameBtn"))).Click();
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

    /// <summary>Returns true when the dashboard is shown (i.e. not on SetDisplayName page).</summary>
    public bool IsOnDashboard()
    {
        try
        {
            return _wait.Until(d =>
                d.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    /// <summary>Returns the current value of the display name input on the dashboard settings form.</summary>
    public string GetDashboardDisplayNameInputValue()
    {
        try
        {
            var input = _wait.Until(d => d.FindElement(By.Id("displayNameInput")));
            return input.GetAttribute("value") ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    /// <summary>Updates the display name via the account settings form.</summary>
    public void UpdateDisplayNameFromDashboard(string displayName)
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/dashboard/settings");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        var input = _wait.Until(d => d.FindElement(By.Id("displayNameInput")));
        input.Clear();
        input.SendKeys(displayName);

        // Use JS click to avoid stale-element reference from RobustWebElement's TagName diagnostic.
        var btn = _wait.Until(d => d.FindElement(By.Id("updateDisplayNameBtn")));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", btn);
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

    /// <summary>Returns the display name shown in the nav bar.</summary>
    public string GetDisplayedDisplayName()
    {
        try
        {
            // Wait until the nav display name element is present and contains non-empty text
            var text = _wait.Until(d =>
            {
                var els = d.FindElements(By.Id("navDisplayNameText"));
                if (els.Count == 0) return null;
                var t = els[0].Text?.Trim() ?? string.Empty;
                return t.Length > 0 ? t : null;
            });

            return text ?? string.Empty;
        }
        catch
        {
            TestContext.Out.WriteLine($"[{nameof(GetDisplayedDisplayName)}] FindElements(By.Id('navDisplayNameText')) Failed or text empty.");
            return string.Empty;
        }
    }

    /// <summary>Returns true when the display name success banner is shown on the dashboard.</summary>
    public bool IsDisplayNameSuccessVisible()
    {
        try
        {
            return _wait.Until(d =>
                d.FindElements(By.Id("displayNameSuccessMessage")).Count > 0);
        }
        catch { return false; }
    }

    /// <summary>
    /// Attempts to log in with the given credentials and returns immediately without
    /// waiting for the user dropdown — used for UNSET-display-name scenarios where
    /// the user is redirected to SetDisplayName instead of Dashboard.
    /// </summary>
    public void AttemptLoginNoWait(string email, string password)
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        _wait.Until(d => d.FindElement(By.Id("emailField"))).SendKeys(email);
        _wait.Until(d => d.FindElement(By.Id("passwordField"))).SendKeys(password);

        _wait.Until(d =>
        {
            var btn = d.FindElements(By.Id("submitBtn"));
            return btn.Count > 0 && btn[0].Enabled;
        });

        _wait.Until(d => d.FindElement(By.Id("submitBtn"))).Click();

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
