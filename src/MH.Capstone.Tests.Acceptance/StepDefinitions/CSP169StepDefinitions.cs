using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP169StepDefinitions
{
    private readonly AuthenticationDriver _authenticationDriver;
    private readonly NotificationPreferencesDriver _preferencesDriver;

    public CSP169StepDefinitions(
        AuthenticationDriver authenticationDriver,
        NotificationPreferencesDriver preferencesDriver)
    {
        _authenticationDriver = authenticationDriver;
        _preferencesDriver = preferencesDriver;
    }

    [Given("Alex is logged in")]
    public void GivenAlexIsLoggedIn()
    {
        _authenticationDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("Alex navigates to the notification preferences page")]
    public void WhenAlexNavigatesToNotificationPreferences()
    {
        _preferencesDriver.NavigateToNotificationPreferences();
    }

    [Then("the notification preferences form is displayed")]
    public void ThenTheFormIsDisplayed()
    {
        Assert.That(_preferencesDriver.IsOnNotificationPreferencesPage(), Is.True,
            "Expected to be on notification preferences page");
        Assert.That(_preferencesDriver.PreferencesFormIsVisible(), Is.True,
            "Expected the preferences form to be visible");
    }

    [Then("the System Critical Notifications type is not visible")]
    public void ThenSystemCriticalIsNotVisible()
    {
        Assert.That(_preferencesDriver.SystemCriticalTypeIsNotVisible(), Is.True,
            "System/Account Critical Notifications type should not appear in the UI");
    }

    [When(@"Alex sets ""(.*)"" to ""(.*)""")]
    public void WhenAlexSetsDeliveryChannel(string notificationTypeLabel, string channelLabel)
    {
        _preferencesDriver.SetDeliveryChannel(notificationTypeLabel, channelLabel);
    }

    [When("Alex saves the notification preferences")]
    public void WhenAlexSavesPreferences()
    {
        _preferencesDriver.ClickSave();
    }

    [Then("a success message is shown on the notification preferences page")]
    public void ThenSuccessMessageIsShown()
    {
        Assert.That(_preferencesDriver.SuccessBannerIsVisible(), Is.True,
            "Expected a success banner after saving preferences");
    }

    [Then(@"the ""(.*)"" preference is saved as ""(.*)""")]
    public void ThenPreferenceIsSaved(string notificationTypeLabel, string expectedChannel)
    {
        var actual = _preferencesDriver.GetSelectedChannel(notificationTypeLabel);
        Assert.That(actual, Is.EqualTo(expectedChannel),
            $"Expected '{notificationTypeLabel}' to be set to '{expectedChannel}' but was '{actual}'");
    }
}
