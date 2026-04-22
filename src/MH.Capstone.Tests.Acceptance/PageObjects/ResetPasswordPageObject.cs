using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class ResetPasswordPageObject
{
    private readonly IWebDriver _driver;

    public IWebElement NewPasswordInput      => _driver.FindElement(By.Id("newPasswordField"));
    public IWebElement ConfirmPasswordInput  => _driver.FindElement(By.Id("confirmPasswordField"));
    public IWebElement ResetPasswordBtn      => _driver.FindElement(By.Id("resetPasswordBtn"));

    public bool HasResetForm =>
        _driver.FindElements(By.Id("resetPasswordBtn")).Count > 0;

    public bool HasInvalidLinkMessage =>
        _driver.FindElements(By.Id("invalidResetLinkMessage")).Count > 0;

    public bool HasInlineError =>
        _driver.FindElements(By.Id("resetPasswordError")).Count > 0 &&
        !string.IsNullOrWhiteSpace(_driver.FindElement(By.Id("resetPasswordError")).Text);

    public bool HasRequestNewLinkOption =>
        _driver.FindElements(By.Id("requestNewResetLinkBtn")).Count > 0 ||
        _driver.FindElements(By.LinkText("Request a New Reset Link")).Count > 0 ||
        _driver.FindElements(By.LinkText("request a new reset link")).Count > 0;

    public ResetPasswordPageObject(IWebDriver driver)
    {
        _driver = driver;
    }
}
