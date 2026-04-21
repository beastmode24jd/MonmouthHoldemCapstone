using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Helpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "forgotPassword")]
[ExcludeFromCodeCoverage]
public class CSP26StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly PasswordResetDriver _passwordResetDriver;

    public CSP26StepDefinitions(IWebDriver driver, WebDriverWait wait,
        AcceptanceTestSettings settings, PasswordResetDriver passwordResetDriver)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _passwordResetDriver = passwordResetDriver;
    }

    [Given(@"an anonymous user navigates to the login page")]
    public void GivenAnAnonymousUserNavigatesToTheLoginPage()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    [When(@"the user views the login form")]
    public void WhenTheUserViewsTheLoginForm()
    {
        var form = _wait.Until(d => d.FindElement(By.Id("loginForm")));
        form.Displayed.Should().BeTrue("the login form should be visible on the login page");
    }

    [Then(@"a ""Forgot Password?"" link is visible")]
    public void ThenAForgotPasswordLinkIsVisible()
    {
        var link = _wait.Until(d => d.FindElement(By.LinkText("Forgot Password?")));
        link.Displayed.Should().BeTrue("the 'Forgot Password?' link should be visible on the login page");
    }

    [Then(@"the link changes appearance on hover")]
    public void ThenTheLinkChangesAppearanceOnHover()
    {
        var link = _wait.Until(d => d.FindElement(By.LinkText("Forgot Password?")));
        var initialColor = link.GetCssValue("color");

        new Actions(_driver).MoveToElement(link).Perform();

        var hoverColor  = link.GetCssValue("color");
        var hoverCursor = link.GetCssValue("cursor");

        hoverColor.Should().NotBe(initialColor, "the link color should change on hover");
        hoverCursor.Should().Be("pointer", "the cursor should be a pointer over the link");
    }

    [Given(@"the user is on the reset password form for ""(.*)""")]
    public void GivenTheUserIsOnTheResetPasswordFormFor(string email)
    {
        var link = _passwordResetDriver.GetPasswordResetLink(email);
        _passwordResetDriver.NavigateToResetLink(link);
    }

    [When(@"the user enters new password ""(.*)"" and confirmation ""(.*)""")]
    public void WhenTheUserEntersNewPasswordAndConfirmation(string newPassword, string confirmPassword)
    {
        _wait.Until(d => d.FindElement(By.Id("newPasswordField"))).SendKeys(newPassword);
        _wait.Until(d => d.FindElement(By.Id("confirmPasswordField"))).SendKeys(confirmPassword);
    }

    [When(@"the user submits the reset form")]
    public void WhenTheUserSubmitsTheResetForm()
    {
        _wait.Until(d => d.FindElement(By.Id("resetPasswordBtn"))).Click();
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    [Then(@"a password confirmation mismatch error is visible")]
    public void ThenAPasswordConfirmationMismatchErrorIsVisible()
    {
        _passwordResetDriver.HasConfirmPasswordMismatchError()
            .Should().BeTrue("a mismatch error should appear when the passwords do not match");
    }
}
