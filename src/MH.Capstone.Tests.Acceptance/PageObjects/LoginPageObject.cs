using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class LoginPageObject
{
    public IWebElement UsernameInput => _usernameInput.Value;
    private readonly Lazy<IWebElement> _usernameInput;

    public IWebElement PasswordInput => _passwordInput.Value;
    private readonly Lazy<IWebElement> _passwordInput;

    public IWebElement RememberMeCheck => _rememberMeCheck.Value;
    private readonly Lazy<IWebElement> _rememberMeCheck;

    public IWebElement SubmitBtn => _submitBtn.Value;
    private readonly Lazy<IWebElement> _submitBtn;

    public LoginPageObject(IWebDriver webDriver, string baseUrl)
    {
        var url = $"{baseUrl.TrimEnd('/')}/Account/Login";

        if (!string.Equals(webDriver.Url, url, StringComparison.InvariantCultureIgnoreCase))
            webDriver.Navigate().GoToUrl(url);

        _usernameInput  = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("Email")));
        _passwordInput  = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("passwordField")));
        _rememberMeCheck = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("RememberMe")));
        _submitBtn       = new Lazy<IWebElement>(() => webDriver.FindElement(By.Id("submitBtn")));
    }
}
