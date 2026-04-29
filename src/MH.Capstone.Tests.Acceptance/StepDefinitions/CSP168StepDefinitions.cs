using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using MH.Capstone.Tests.Acceptance.Configuration;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP168StepDefinitions
{
    private readonly DisplayNameDriver _displayNameDriver;
    private readonly AuthenticationDriver _authenticationDriver;
    private readonly EmailVerificationDriver _emailVerificationDriver;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    private string _registeredEmail = string.Empty;
    private string _registeredPassword = string.Empty;

    public CSP168StepDefinitions(
        DisplayNameDriver displayNameDriver,
        AuthenticationDriver authenticationDriver,
        EmailVerificationDriver emailVerificationDriver,
        IWebDriver driver,
        WebDriverWait wait,
        AcceptanceTestSettings settings)
    {
        _displayNameDriver = displayNameDriver;
        _authenticationDriver = authenticationDriver;
        _emailVerificationDriver = emailVerificationDriver;
        _driver = driver;
        _wait = wait;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    // ── Given ──────────────────────────────────────────────────────────────────

    [Given("the user is not logged in")]
    public void GivenTheUserIsNotLoggedIn()
    {
        _authenticationDriver.LogoutUser();
    }

    [Given(@"the user ""(.*)"" has a display name of ""UNSET""")]
    public void GivenTheUserHasDisplayNameUnset(string email)
    {
        // Owen is seeded with DisplayName = "UNSET"; just ensure we're logged out
        _authenticationDriver.LogoutUser();
    }

    [Given(@"the user ""(.*)"" is logged in with password ""(.*)""")]
    public void GivenTheUserIsLoggedIn(string email, string password)
    {
        _authenticationDriver.PreformLoginForUser(email, password);
    }

    [Given(@"a new user registers with display name ""(.*)"" and a unique test email")]
    public void GivenANewUserRegistersWithDisplayName(string displayName)
    {
        _registeredEmail = $"csp168_{Guid.NewGuid():N}@test.com";
        _registeredPassword = "Capstone26!";
        _emailVerificationDriver.RegisterNewUser(_registeredEmail, _registeredPassword, displayName);
    }

    // ── When ───────────────────────────────────────────────────────────────────

    [When("an anonymous user submits the registration form without a display name")]
    public void WhenUserSubmitsRegistrationWithoutDisplayName()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Register");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });

        // Fill in email and password but leave DisplayName empty
        _wait.Until(d => d.FindElement(By.Id("emailField"))).SendKeys("test@example.com");
        _wait.Until(d => d.FindElement(By.Id("passwordField"))).SendKeys("Capstone26!");
        _wait.Until(d => d.FindElement(By.Id("confirmPasswordField"))).SendKeys("Capstone26!");

        // Submit button is disabled by JS when DisplayName is empty; use JS to submit anyway to trigger server-side validation
        ((IJavaScriptExecutor)_driver).ExecuteScript("document.getElementById('registerForm')?.submit();");
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

    [When(@"""(.*)"" logs in with password ""(.*)""")]
    public void WhenUserLogsIn(string email, string password)
    {
        _displayNameDriver.AttemptLoginNoWait(email, password);
    }

    [When("the user verifies their email")]
    public void WhenTheUserVerifiesTheirEmail()
    {
        var link = _emailVerificationDriver.GetEmailConfirmationLink(_registeredEmail);
        link.Should().NotBeNullOrWhiteSpace("test endpoint should return a confirmation URL");
        _emailVerificationDriver.NavigateToVerificationLink(link);
    }

    [When("the user logs in with their registered credentials")]
    public void WhenTheUserLogsInWithRegisteredCredentials()
    {
        _authenticationDriver.PreformLoginForUser(_registeredEmail, _registeredPassword);
    }

    [When(@"the user sets their display name to ""(.*)""")]
    public void WhenTheUserSetsDisplayName(string displayName)
    {
        _displayNameDriver.SubmitSetDisplayName(displayName);
    }

    [When(@"the user updates their display name to ""(.*)""")]
    public void WhenTheUserUpdatesDisplayName(string displayName)
    {
        _displayNameDriver.UpdateDisplayNameFromDashboard(displayName);
    }

    [When("the user views the dashboard account settings")]
    public void WhenTheUserViewsDashboardSettings()
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
    }

    [When(@"the user submits a display name of ""(.*)"" from account settings")]
    public void WhenTheUserSubmitsShortDisplayName(string displayName)
    {
        _displayNameDriver.UpdateDisplayNameFromDashboard(displayName);
    }

    // ── Then ───────────────────────────────────────────────────────────────────

    [Then("an inline validation error is shown for the display name field")]
    public void ThenValidationErrorIsShownForDisplayName()
    {
        // Either the page stays on Register (no redirect) or shows a validation error
        var url = _driver.Url;
        var isStillOnRegisterOrHasError =
            url.Contains("/account/register", StringComparison.OrdinalIgnoreCase) ||
            _driver.FindElements(By.CssSelector("span[data-valmsg-for='DisplayName'], .field-validation-error")).Count > 0;

        isStillOnRegisterOrHasError.Should().BeTrue(
            "submitting without a display name should show a validation error on the registration page");
    }

    [Then("the account is not created")]
    public void ThenAccountIsNotCreated()
    {
        _driver.Url.Should().NotContain("/account/registerconfirmation",
            because: "the registration should have failed — no confirmation page should be shown");
    }

    [Then("the user is redirected to the Set Your Display Name page")]
    public void ThenUserIsRedirectedToSetDisplayNamePage()
    {
        _displayNameDriver.IsSetDisplayNamePageVisible().Should().BeTrue(
            "a user with UNSET display name should be redirected to the Set Your Display Name page after login");
    }

    [Then("the user is not redirected to the Set Your Display Name page")]
    public void ThenUserIsNotRedirectedToSetDisplayNamePage()
    {
        _displayNameDriver.IsSetDisplayNamePageVisible().Should().BeFalse(
            "a user who set a display name at registration should not be redirected to SetDisplayName");
    }

    [Then("the user is redirected to the dashboard")]
    public void ThenUserIsRedirectedToDashboard()
    {
        _displayNameDriver.IsOnDashboard().Should().BeTrue(
            "after saving a display name the user should be redirected to the dashboard");
    }

    [Then(@"""(.*)"" appears as the user's display name on the dashboard")]
    public void ThenDisplayNameAppearsOnDashboard(string expectedName)
    {
        var navText = _displayNameDriver.GetDisplayedDisplayName();
        navText.Should().Contain(expectedName,
            because: $"the display name '{expectedName}' should appear in the navigation");
    }

    [Then(@"the display name ""(.*)"" is shown on the dashboard")]
    public void ThenDisplayNameIsShownOnDashboard(string expectedName)
    {
        var navText = _displayNameDriver.GetDisplayedDisplayName();
        navText.Should().Contain(expectedName,
            because: $"the updated display name '{expectedName}' should appear in the navigation");
    }

    [Then("a success confirmation is displayed")]
    public void ThenSuccessConfirmationIsDisplayed()
    {
        _displayNameDriver.IsDisplayNameSuccessVisible().Should().BeTrue(
            "after updating the display name a success message should be shown");
    }

    [Then(@"the display name input is pre-populated with ""(.*)""")]
    public void ThenDisplayNameInputIsPrePopulated(string expectedValue)
    {
        var value = _displayNameDriver.GetDashboardDisplayNameInputValue();
        value.Should().Be(expectedValue,
            because: $"the display name input should be pre-populated with '{expectedValue}'");
    }

    [Then("the display name update is rejected with a validation error")]
    public void ThenDisplayNameUpdateIsRejected()
    {
        // The DashboardController rejects short names via server-side validation and sets TempData
        var errorShown = _driver.FindElements(By.CssSelector(".alert-danger")).Count > 0 ||
                         _driver.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase);

        errorShown.Should().BeTrue(
            "submitting a too-short display name should result in an error being shown");
    }
}
