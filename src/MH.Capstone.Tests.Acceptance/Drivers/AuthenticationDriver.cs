using System.Diagnostics.CodeAnalysis;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class AuthenticationDriver
{
    private readonly IWebDriver _webDriver;
    private readonly string _baseUrl;

    public AuthenticationDriver(IWebDriver webDriver, AcceptanceTestSettings settings)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    public bool IsUserLoggedIn(string? username = null)
    {
        _webDriver.Navigate().GoToUrl(_baseUrl);

        try
        {
            var userElement = _webDriver.FindElement(By.Id("userDropdownNavDisplay"));
            return string.IsNullOrEmpty(username) || userElement.Text.Contains(username);
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public void PreformLoginForUser(string username, string password)
    {
        if (IsUserLoggedIn(username))
            return;

        // If a different user is logged in, log them out first before logging in.
        LogoutUser();

        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Attempting User {username} log in.");
        var loginPage = new LoginPageObject(_webDriver, _baseUrl);
        loginPage.UsernameInput.SendKeys(username);
        loginPage.PasswordInput.SendKeys(password);
        loginPage.SubmitBtn.Click();
        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Confirming User {username} log in.");

        // After submitting the form, wait for either a validation error to appear on the login
        // page (server returned model errors) or for the page to redirect/refresh and show the
        // user dropdown (meaning sign in was successful).
        var loginUrl = $"{_baseUrl.TrimEnd('/')}/Account/Login";
        var timeout = TimeSpan.FromSeconds(10);
        var sw = Stopwatch.StartNew();
        var loginSuccess = false;
        var errorDisplayed = false;

        while (sw.Elapsed < timeout)
        {
            try
            {
                // If we've been redirected away from the login URL, check for the user dropdown
                // element which indicates a logged-in user.
                if (!string.Equals(_webDriver.Url, loginUrl, StringComparison.InvariantCultureIgnoreCase))
                {
                    var userElems = _webDriver.FindElements(By.Id("userDropdownNavDisplay"));
                    if (userElems.Count > 0)
                    {
                        var text = userElems.First().Text ?? string.Empty;
                        if (string.IsNullOrEmpty(username) || text.Contains(username))
                        {
                            loginSuccess = true;
                            break;
                        }
                    }
                }

                // Check for server-side validation errors rendered on the login page
                var errorElems = _webDriver.FindElements(By.CssSelector(".alert.alert-danger"));
                if (errorElems.Count > 0)
                {
                    errorDisplayed = true;
                    break;
                }
            }
            catch (StaleElementReferenceException)
            {
                // Element references can become stale during navigation; ignore and retry.
            }

            Thread.Sleep(250);
        }

        sw.Stop();
        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] Login attempt for user {username} completed after {sw.Elapsed.TotalSeconds:N2} seconds.");

        if (!loginSuccess)
        {
            if (errorDisplayed)
                throw new Exception($"Failed to log in user '{username}': server returned validation errors.");

            throw new Exception($"Failed to log in user '{username}' (timeout waiting for login or error).");
        }

        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] User {username} logged in.");
    }

    public void LogoutUser()
    {
        if (!IsUserLoggedIn()) return;
        var logoutForm = _webDriver.FindElement(By.Id("logoutForm"));
        logoutForm.Submit();
        TestContext.Out.WriteLine($"[{nameof(AuthenticationDriver)}] User logged out.");
    }

    public bool WasPageAccessDenied(string urlToTest)
    {
        _webDriver.Navigate().GoToUrl(urlToTest);
        return WasPageAccessDenied();
    }

    public bool WasPageAccessDenied() =>
        _webDriver.Url.Contains("/account/login", StringComparison.InvariantCultureIgnoreCase);
}
