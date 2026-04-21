using System.Diagnostics.CodeAnalysis;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class AuthenticationDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public AuthenticationDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public bool IsUserLoggedIn(string? username = null)
    {
        _webDriver.Navigate().GoToUrl(_baseUrl);
        try
        {
            // Wait briefly for the user dropdown to appear. If it doesn't appear within the
            // timeout, treat as not logged in.
            var userElement = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(2)).Until(d =>
            {
                var elems = d.FindElements(By.Id("userDropdownNavDisplay"));
                TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] User dropdown elements found: {elems.Count}");
                return elems.Count > 0 ? elems[0] : null;
            });

            TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] User Auth status: " +
                                      $"{string.IsNullOrEmpty(username) || userElement?.Text.Contains(username) == true}.");
            return string.IsNullOrEmpty(username) || userElement?.Text.Contains(username) == true;
        }
        catch
        {
            TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] User Auth status: false.");
            return false;
        }
    }

    public void PreformLoginForUser(string username, string password)
    {
        var loginUrl = $"{_baseUrl.TrimEnd('/')}/Account/Login";

        if (IsUserLoggedIn(username))
            return;

        // If a different user is logged in, log them out first before logging in.
        LogoutUser();

        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Attempting User {username} log in.");
        _webDriver.Navigate().GoToUrl(loginUrl);
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        var loginPage = new LoginPageObject(_webDriver, _baseUrl);
        loginPage.UsernameInput.SendKeys(username);
        loginPage.PasswordInput.SendKeys(password);

        try
        {
            // Diagnostic: log the values in the inputs and whether the submit button is enabled
            var emailVal = ((IJavaScriptExecutor)_webDriver).ExecuteScript("return document.getElementById('emailField')?.value")?.ToString();
            var passVal = ((IJavaScriptExecutor)_webDriver).ExecuteScript("return document.getElementById('passwordField')?.value")?.ToString();
            var submitDisabled = ((IJavaScriptExecutor)_webDriver).ExecuteScript("return document.getElementById('submitBtn')?.disabled")?.ToString();
            TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Diagnostic: email='{emailVal}' passwordPresent={(string.IsNullOrEmpty(passVal) ? "false" : "true")} submitDisabled={submitDisabled}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Diagnostic JS check failed: {ex.Message}");
        }

        loginPage.SubmitBtn.Click();
        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Confirming User {username} log in.");

        // After submitting the form, wait for either a validation error to appear on the login
        // page (server returned model errors) or for the page to redirect/refresh and show the
        // user dropdown (meaning sign in was successful).
        try
        {
            _wait.Until(d =>
            {
                // If we've been redirected away from the login URL,
                // check for if the user shows logged in
                if (!string.Equals(d.Url, loginUrl, StringComparison.InvariantCultureIgnoreCase)
                    && IsUserLoggedIn(username))
                {
                    return true;
                }

                // Check for server-side validation errors rendered on the login page
                    var errorElems = d.FindElements(By.CssSelector(".alert.alert-danger"));
                if (errorElems.Count > 0)
                    throw new ValidationException($"Failed to log in user '{username}': server returned validation errors.");

                // keep waiting
                return false;
            });
        }
        catch(Exception e)
        {
            // timeout or other waiting error -> treat as failed login
            throw new Exception($"Failed to log in user '{username}' (timeout waiting for login or unhandled exception).",
                e);
        }

        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Login attempt for user {username} completed.");
        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] User {username} logged in.");
    }

    public void LogoutUser()
    {
        if (!IsUserLoggedIn()) return;
        var logoutForm = _wait.Until(d => d.FindElement(By.Id("logoutForm")));
        logoutForm.Submit();
        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] User logged out.");
    }

    public bool WasPageAccessDenied(string urlToTest)
    {
        _webDriver.Navigate().GoToUrl(urlToTest);
        try
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
        catch
        {
            // ignore
        }

        return WasPageAccessDenied();
    }

    public bool WasPageAccessDenied()
    {
        try
        {
            return _wait.Until(d =>
                d.Url.Contains("/account/login", StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
