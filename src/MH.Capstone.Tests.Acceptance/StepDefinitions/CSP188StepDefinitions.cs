using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP188StepDefinitions
{
    private readonly AuthenticationDriver _authenticationDriver;
    private readonly AccountSettingsDriver _accountSettingsDriver;

    public CSP188StepDefinitions(
        AuthenticationDriver authenticationDriver,
        AccountSettingsDriver accountSettingsDriver)
    {
        _authenticationDriver = authenticationDriver;
        _accountSettingsDriver = accountSettingsDriver;
    }

    [Given("Alex is logged in for settings")]
    public void GivenAlexIsLoggedInForSettings()
    {
        _authenticationDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [Given("Alex is logged out")]
    public void GivenAlexIsLoggedOut()
    {
        _authenticationDriver.LogoutUser();
    }

    [When("Alex visits the dashboard")]
    public void WhenAlexVisitsTheDashboard()
    {
        _accountSettingsDriver.NavigateToDashboard();
    }

    [When("Alex clicks the Account Settings link")]
    public void WhenAlexClicksAccountSettingsLink()
    {
        _accountSettingsDriver.ClickAccountSettingsLink();
    }

    [When("Alex navigates to the account settings page")]
    public void WhenAlexNavigatesToAccountSettingsPage()
    {
        _accountSettingsDriver.NavigateToSettings();
    }

    [When("an unauthenticated user navigates to /dashboard/settings")]
    public void WhenUnauthenticatedUserNavigatesToSettings()
    {
        _accountSettingsDriver.NavigateToSettings();
    }

    [Then("Alex is on the account settings page")]
    public void ThenAlexIsOnAccountSettingsPage()
    {
        Assert.That(_accountSettingsDriver.IsOnSettingsPage(), Is.True,
            "Expected to be on /dashboard/settings");
    }

    [Then("the display name form is visible")]
    public void ThenDisplayNameFormIsVisible()
    {
        Assert.That(_accountSettingsDriver.DisplayNameFormIsVisible(), Is.True,
            "Expected the display name form to be visible on the settings page");
    }

    [Then("the notification preferences link is visible")]
    public void ThenNotificationPreferencesLinkIsVisible()
    {
        Assert.That(_accountSettingsDriver.NotificationPreferencesLinkIsVisible(), Is.True,
            "Expected the notification preferences link to be visible on the settings page");
    }

    [Then("the account settings forms are not shown on the dashboard")]
    public void ThenAccountSettingsFormsAreAbsent()
    {
        Assert.That(_accountSettingsDriver.AccountSettingsFormsAreAbsentFromDashboard(), Is.True,
            "Expected the account settings forms (display name input) to be absent from the dashboard");
    }

    [Then("the Account Settings link is visible on the dashboard")]
    public void ThenAccountSettingsLinkIsVisible()
    {
        Assert.That(_accountSettingsDriver.AccountSettingsLinkIsOnDashboard(), Is.True,
            "Expected an Account Settings link to be present on the dashboard");
    }

    [Then("they are redirected to the login page")]
    public void ThenTheyAreRedirectedToLogin()
    {
        Assert.That(_accountSettingsDriver.IsOnLoginPage(), Is.True,
            "Expected unauthenticated user to be redirected to the login page");
    }
}
