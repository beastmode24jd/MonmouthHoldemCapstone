using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP133StepDefinitions
{
    private readonly PasswordResetDriver _passwordResetDriver;
    private readonly AuthenticationDriver _authenticationDriver;

    // Stores reset links between Given/When/Then steps
    private string? _firstResetLink;
    private string? _secondResetLink;

    public CSP133StepDefinitions(
        PasswordResetDriver passwordResetDriver,
        AuthenticationDriver authenticationDriver)
    {
        _passwordResetDriver = passwordResetDriver;
        _authenticationDriver = authenticationDriver;
    }

    // ── Scenario 1: Password Reset Email Request ─────────────────────────────

    [Given("an anonymous user is on the Forgot Password page")]
    public void GivenAnAnonymousUserIsOnTheForgotPasswordPage()
    {
        _authenticationDriver.LogoutUser();
    }

    [When("they submit a password reset request with email {string}")]
    public void WhenTheySubmitAPasswordResetRequestWithEmail(string email)
    {
        _passwordResetDriver.SubmitForgotPasswordRequest(email);
    }

    [Then("they see a confirmation message that does not reveal whether the email is registered")]
    public void ThenTheySeeAConfirmationMessageThatDoesNotRevealWhetherTheEmailIsRegistered()
    {
        _passwordResetDriver.IsEmailSentBannerVisible().Should().BeTrue(
            "the confirmation banner should appear regardless of whether the email is registered");
    }

    // ── Scenario 2: Reset Link Validity ──────────────────────────────────────

    [Given("a valid password reset link has been generated for {string}")]
    public void GivenAValidPasswordResetLinkHasBeenGeneratedFor(string email)
    {
        _firstResetLink = _passwordResetDriver.GetPasswordResetLink(email);
        _firstResetLink.Should().NotBeNullOrWhiteSpace("test endpoint should return a reset URL");
    }

    [When("the user navigates to the reset link")]
    public void WhenTheUserNavigatesToTheResetLink()
    {
        _passwordResetDriver.NavigateToResetLink(_firstResetLink!);
    }

    [Then("the user sees the password reset form")]
    public void ThenTheUserSeesThePasswordResetForm()
    {
        _passwordResetDriver.IsResetPasswordFormVisible().Should().BeTrue(
            "navigating to a valid reset link should show the password reset form");
    }

    // ── Scenario 3: Successful Password Reset ────────────────────────────────

    [When("the user submits new password {string} confirmed as {string}")]
    public void WhenTheUserSubmitsNewPasswordConfirmedAs(string newPassword, string confirmPassword)
    {
        _passwordResetDriver.NavigateToResetLinkAndSubmit(_firstResetLink!, newPassword, confirmPassword);
    }

    [Then("the user is redirected to the login page with a password reset success message")]
    public void ThenTheUserIsRedirectedToTheLoginPageWithAPasswordResetSuccessMessage()
    {
        _passwordResetDriver.IsPasswordResetSuccessBannerOnLoginPage().Should().BeTrue(
            "a successful password reset should redirect to the login page with a success message");
    }

    [Then("the user can log in with email {string} and password {string}")]
    public void ThenTheUserCanLogInWithEmailAndPassword(string email, string password)
    {
        _authenticationDriver.PreformLoginForUser(email, password);
        _authenticationDriver.IsUserLoggedIn().Should().BeTrue(
            $"the user should be able to log in with the new password");
        _authenticationDriver.LogoutUser();
    }

    // ── Scenario 4: Invalid or Expired Reset Link ─────────────────────────────

    [Given("the user navigates to a reset link with an invalid token for {string}")]
    public void GivenTheUserNavigatesToAResetLinkWithAnInvalidTokenFor(string email)
    {
        // Build a URL with a syntactically valid but semantically wrong Base64Url token
        var fakeToken = "aW52YWxpZC10b2tlbi1hYmMxMjM";  // Base64Url of "invalid-token-abc123"
        var path = $"Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={fakeToken}";
        _passwordResetDriver.NavigateToPath(path);
    }

    [Then("the user sees an invalid reset link error message")]
    public void ThenTheUserSeesAnInvalidResetLinkErrorMessage()
    {
        // May be the dedicated error page OR an inline validation error on the form
        var invalidPage   = _passwordResetDriver.IsInvalidLinkPageVisible();
        var inlineError   = _passwordResetDriver.IsInvalidTokenInlineErrorVisible();
        (invalidPage || inlineError).Should().BeTrue(
            "the user should see a clear error when the reset link is invalid or expired");
    }

    [Then("the user sees an option to request a new reset link")]
    public void ThenTheUserSeesAnOptionToRequestANewResetLink()
    {
        _passwordResetDriver.HasRequestNewLinkOption().Should().BeTrue(
            "the page should offer a way to request a fresh reset link");
    }

    // ── Scenario 5: Multiple Reset Requests Invalidate Previous Tokens ────────

    [Given("a first password reset link has been generated for {string}")]
    public void GivenAFirstPasswordResetLinkHasBeenGeneratedFor(string email)
    {
        _firstResetLink = _passwordResetDriver.GetPasswordResetLink(email);
        _firstResetLink.Should().NotBeNullOrWhiteSpace();
    }

    [Given("a second password reset link has been generated for {string}")]
    public void GivenASecondPasswordResetLinkHasBeenGeneratedFor(string email)
    {
        _secondResetLink = _passwordResetDriver.GetPasswordResetLink(email);
        _secondResetLink.Should().NotBeNullOrWhiteSpace();
    }

    [When("the user navigates to the first reset link and submits password {string}")]
    public void WhenTheUserNavigatesToTheFirstResetLinkAndSubmitsPassword(string password)
    {
        _passwordResetDriver.NavigateToResetLinkAndSubmit(_firstResetLink!, password, password);
    }
}
