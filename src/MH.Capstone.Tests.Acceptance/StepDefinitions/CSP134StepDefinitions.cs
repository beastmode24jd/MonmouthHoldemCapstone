using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP134StepDefinitions
{
    private readonly EmailVerificationDriver _emailVerificationDriver;
    private readonly AuthenticationDriver    _authenticationDriver;

    /// <summary>
    /// Unique email generated once per scenario so multiple scenarios can run
    /// without conflicting in the shared database.
    /// </summary>
    private string _registeredEmail = string.Empty;

    public CSP134StepDefinitions(
        EmailVerificationDriver emailVerificationDriver,
        AuthenticationDriver authenticationDriver)
    {
        _emailVerificationDriver = emailVerificationDriver;
        _authenticationDriver    = authenticationDriver;
    }

    // ── Shared Given ──────────────────────────────────────────────────────────

    [Given("a new user registers with a unique test email and password {string}")]
    public void GivenANewUserRegistersWithAUniqueTestEmailAndPassword(string password)
    {
        _authenticationDriver.LogoutUser();

        // Generate a unique email so scenarios do not interfere with each other
        // even when the database is not wiped between individual scenarios.
        _registeredEmail = $"csp134_{Guid.NewGuid():N}@test.com";

        _emailVerificationDriver.RegisterNewUser(_registeredEmail, password);
    }

    // ── Scenario 1: Email Sent on Registration ────────────────────────────────

    [Then("they are shown a registration confirmation page asking them to check their email")]
    public void ThenTheyAreShownARegistrationConfirmationPageAskingThemToCheckTheirEmail()
    {
        _emailVerificationDriver.IsRegistrationConfirmationVisible().Should().BeTrue(
            "after successful registration the app should show a 'check your email' confirmation page");
    }

    // ── Scenario 2: Verification Link Works ───────────────────────────────────

    [Given("an email confirmation link has been generated for the registered email")]
    public void GivenAnEmailConfirmationLinkHasBeenGeneratedForTheRegisteredEmail()
    {
        // No-op — stored in _registeredEmail; the link is fetched in the When step
    }

    [When("the user navigates to the email confirmation link")]
    public void WhenTheUserNavigatesToTheEmailConfirmationLink()
    {
        var link = _emailVerificationDriver.GetEmailConfirmationLink(_registeredEmail);
        link.Should().NotBeNullOrWhiteSpace("test endpoint should return a confirmation URL");
        _emailVerificationDriver.NavigateToVerificationLink(link);
    }

    [Then("the user sees a successful email verification message")]
    public void ThenTheUserSeesASuccessfulEmailVerificationMessage()
    {
        _emailVerificationDriver.IsVerificationSuccessVisible().Should().BeTrue(
            "navigating to a valid confirmation link should show a success message");
    }

    [Then("the user can log in with their registered email and password {string}")]
    public void ThenTheUserCanLogInWithTheirRegisteredEmailAndPassword(string password)
    {
        _authenticationDriver.PreformLoginForUser(_registeredEmail, password);
        _authenticationDriver.IsUserLoggedIn().Should().BeTrue(
            "a verified user should be able to log in");
        _authenticationDriver.LogoutUser();
    }

    // ── Scenario 3: Invalid or Expired Verification Link ─────────────────────

    [When("the user navigates to a verification link with an invalid token")]
    public void WhenTheUserNavigatesToAVerificationLinkWithAnInvalidToken()
    {
        _emailVerificationDriver.NavigateToInvalidVerificationLink(_registeredEmail);
    }

    [Then("the user sees a verification error message")]
    public void ThenTheUserSeesAVerificationErrorMessage()
    {
        _emailVerificationDriver.IsVerificationErrorVisible().Should().BeTrue(
            "an invalid or expired verification link should show a clear error message");
    }

    [Then("the user sees an option to request a new verification link")]
    public void ThenTheUserSeesAnOptionToRequestANewVerificationLink()
    {
        _emailVerificationDriver.HasRequestNewVerificationLinkOption().Should().BeTrue(
            "the error page should offer a way to request a fresh verification link");
    }

    // ── Scenario 4: Restricted Access Before Verification ────────────────────

    [When("the unverified user tries to log in with their registered email and password {string}")]
    public void WhenTheUnverifiedUserTriesToLogInWithTheirRegisteredEmailAndPassword(string password)
    {
        _emailVerificationDriver.AttemptLogin(_registeredEmail, password);
    }

    [Then("the user sees an email verification required message")]
    public void ThenTheUserSeesAnEmailVerificationRequiredMessage()
    {
        _emailVerificationDriver.IsEmailVerificationRequiredMessageVisible().Should().BeTrue(
            "an unverified user should be blocked at login with a clear verification-required message");
    }

    [Then("the user sees an option to resend the verification email")]
    public void ThenTheUserSeesAnOptionToResendTheVerificationEmail()
    {
        _emailVerificationDriver.HasResendVerificationOption().Should().BeTrue(
            "the login page should offer a resend-verification option to blocked users");
    }

    // ── Scenario 5: Resend Verification Email ─────────────────────────────────

    [When("the user submits a resend verification request for their registered email")]
    public void WhenTheUserSubmitsAResendVerificationRequestForTheirRegisteredEmail()
    {
        _emailVerificationDriver.SubmitResendVerification(_registeredEmail);
    }

    [Then("they see a resend confirmation message")]
    public void ThenTheySeeAResendConfirmationMessage()
    {
        _emailVerificationDriver.IsResendConfirmationVisible().Should().BeTrue(
            "after requesting a resend the app should show a 'check your email' confirmation");
    }
}
