using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Helpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "deactivation")]
[ExcludeFromCodeCoverage]
public class CSP47AccountDeactivationSteps
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly AuthenticationDriver _authDriver;
    private readonly EmailVerificationDriver _emailVerificationDriver;

    private const string TestEmail    = "alex@test.com";
    private const string TestPassword = "Capstone26!";

    private string _dynamicTestEmail    = string.Empty;
    private string _dynamicTestPassword = string.Empty;

    public CSP47AccountDeactivationSteps(IWebDriver driver, WebDriverWait wait,
        AcceptanceTestSettings settings, AuthenticationDriver authDriver,
        EmailVerificationDriver emailVerificationDriver)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _authDriver = authDriver;
        _emailVerificationDriver = emailVerificationDriver;
    }

    [Given(@"I am using Chrome browser")]
    public void GivenIAmUsingChromeBrowser()
    {
        // No-op: the shared ChromeDriver is created once in BeforeTestRun (Startup.cs).
    }

    [Given(@"the application is running")]
    public void GivenTheApplicationIsRunning()
    {
        _driver.Navigate().GoToUrl(_baseUrl);
    }

    [Given(@"I am logged in as a registered user")]
    public void GivenIAmLoggedInAsARegisteredUser()
    {
        _authDriver.PreformLoginForUser(TestEmail, TestPassword);
    }

    [Given(@"I have registered a new test account")]
    public void GivenIHaveRegisteredANewTestAccount()
    {
        _dynamicTestEmail    = $"testdeactivate{Guid.NewGuid().ToString("N")[..8]}@test.com";
        _dynamicTestPassword = "TestDeactivate123!";

        _emailVerificationDriver.RegisterNewUser(_dynamicTestEmail, _dynamicTestPassword);
    }

    [Given(@"I am logged in with the test account")]
    public void GivenIAmLoggedInWithTheTestAccount()
    {
        // Post-CSP-134: registration requires email verification before login.
        // Get the confirmation link and navigate to it to verify the email first.
        var link = _emailVerificationDriver.GetEmailConfirmationLink(_dynamicTestEmail);
        _emailVerificationDriver.NavigateToVerificationLink(link);

        _authDriver.PreformLoginForUser(_dynamicTestEmail, _dynamicTestPassword);
    }

    [When(@"I navigate to the deactivate page without logging in")]
    public void WhenINavigateToTheDeactivatePageWithoutLoggingIn()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Deactivate");
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    [When(@"I navigate to the deactivate page")]
    public void WhenINavigateToTheDeactivatePage()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Deactivate");
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    [When(@"I enter an incorrect password ""(.*)""")]
    public void WhenIEnterAnIncorrectPassword(string password)
    {
        var passwordField = _wait.Until(d => d.FindElement(By.Id("Password")));
        passwordField.Clear();
        passwordField.SendKeys(password);
    }

    [When(@"I enter the correct password")]
    public void WhenIEnterTheCorrectPassword()
    {
        var passwordField = _wait.Until(d => d.FindElement(By.Id("Password")));
        passwordField.Clear();
        passwordField.SendKeys(_dynamicTestPassword);
    }

    [When(@"I click the deactivate button")]
    public void WhenIClickTheDeactivateButton()
    {
        var deactivateBtn = _wait.Until(d => d.FindElement(By.CssSelector("button.btn-danger[type='submit']")));
        deactivateBtn.Click();
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
    }

    [Then(@"I should be redirected to the login page")]
    public void ThenIShouldBeRedirectedToTheLoginPage()
    {
        _wait.Until(d => d.Url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
        _driver.Url.Should().ContainEquivalentOf("/account/login");
    }

    [Then(@"I should see a warning about account deactivation consequences")]
    public void ThenIShouldSeeAWarningAboutAccountDeactivationConsequences()
    {
        var warningAlert = _wait.Until(d => d.FindElement(By.CssSelector(".alert-warning")));
        warningAlert.Displayed.Should().BeTrue("a warning alert should be visible on the deactivation page");
        warningAlert.Text.Should().Contain("Warning");
    }

    [Then(@"I should see a password confirmation field")]
    public void ThenIShouldSeeAPasswordConfirmationField()
    {
        var passwordField = _wait.Until(d => d.FindElement(By.Id("Password")));
        passwordField.Displayed.Should().BeTrue("the password confirmation field should be visible");
    }

    [Then(@"I should see an error message about incorrect password")]
    public void ThenIShouldSeeAnErrorMessageAboutIncorrectPassword()
    {
        _driver.WaitForDocumentReady(TimeSpan.FromSeconds(5));
        _driver.PageSource.ToLower().Should().Contain("incorrect",
            "an error about the incorrect password should appear");
    }

    [Then(@"I should remain on the deactivate page")]
    public void ThenIShouldRemainOnTheDeactivatePage()
    {
        _driver.Url.Should().ContainEquivalentOf("/Account/Deactivate");
    }

    [Then(@"I should see a message that my account has been deactivated")]
    public void ThenIShouldSeeAMessageThatMyAccountHasBeenDeactivated()
    {
        // After deactivation the user is redirected to the login page.
        _driver.Url.ToLower().Should().Contain("/account/login");
    }
}
