using System.Diagnostics.CodeAnalysis;
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

        if (!IsUserLoggedIn(username))
            throw new Exception($"Failed to log in user '{username}'.");

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
