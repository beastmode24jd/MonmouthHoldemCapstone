using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Helpers;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "offline-queue")]
[ExcludeFromCodeCoverage]
public class CSP177StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly AuthenticationDriver _authDriver;
    private readonly SightingsDriver _sightingsDriver;
    private readonly OfflineQueueDriver _offlineQueueDriver;

    public CSP177StepDefinitions(
        IWebDriver driver,
        AuthenticationDriver authDriver,
        SightingsDriver sightingsDriver,
        OfflineQueueDriver offlineQueueDriver)
    {
        _driver = driver;
        _authDriver = authDriver;
        _sightingsDriver = sightingsDriver;
        _offlineQueueDriver = offlineQueueDriver;
    }

    [BeforeScenario("offline-queue")]
    public void ResetSession()
    {
        _driver.Manage().Cookies.DeleteAllCookies();
    }

    [AfterScenario("offline-queue")]
    public void Cleanup()
    {
        _offlineQueueDriver.SimulateOnline();
        _offlineQueueDriver.CleanupTempImage();
    }

    [When("Alex navigates to the sighting upload page")]
    public void WhenAlexNavigatesToTheSightingUploadPage()
    {
        _sightingsDriver.NavigateToSightingsUpload();
    }

    [When("the device is simulated as offline")]
    public void WhenTheDeviceIsSimulatedAsOffline()
    {
        _offlineQueueDriver.SimulateOffline();
    }

    [When("Alex fills in the sighting form")]
    public void WhenAlexFillsInTheSightingForm()
    {
        // CSP-189: the upload form rejects Low-tier photos before saving, so the prior
        // solid-gray JPEG would never make it past validation. Use the high-quality
        // stripes preset so the form accepts the upload and the offline-queue flow runs.
        var tempImagePath = TestImageFactory.CreateValid();

        _sightingsDriver.SetImageForUpload(tempImagePath);
        _sightingsDriver.SetSpeciesName("CSP177-OfflineTestSpecies");
        _sightingsDriver.SetLatitude(45.0);
        _sightingsDriver.SetLongitude(-123.0);
        _sightingsDriver.SetTimestamp(DateTimeOffset.Now.AddMinutes(-1));
        _sightingsDriver.SetDescription("CSP-177 offline queue acceptance test.");
    }

    [When("Alex submits the sighting form")]
    public void WhenAlexSubmitsTheSightingForm()
    {
        // Must click the button (not form.submit()) so the JS submit event fires.
        // SightingsDriver.SubmitSightingsForm() uses form.submit() which bypasses
        // all addEventListener("submit", ...) handlers.
        _offlineQueueDriver.ClickSubmitButton();
    }

    [Then("Alex is redirected to the offline queue page")]
    public void ThenAlexIsRedirectedToTheOfflineQueuePage()
    {
        _offlineQueueDriver.IsOnOfflineQueuePage().Should().BeTrue(
            "submitting a sighting while offline should redirect to the offline queue page");
    }

    [Then("the offline queue page shows at least one queued item")]
    public void ThenTheOfflineQueuePageShowsAtLeastOneQueuedItem()
    {
        _offlineQueueDriver.HasQueuedItems().Should().BeTrue(
            "the queue should contain the sighting captured while offline");
    }

    [Given("Alex has a queued offline sighting")]
    public void GivenAlexHasAQueuedOfflineSighting()
    {
        _offlineQueueDriver.NavigateToOfflineQueue();
        _offlineQueueDriver.InjectQueuedSighting("CSP177-PrivacyTestSpecies");
    }

    [When("Alex navigates to the offline queue page")]
    public void WhenAlexNavigatesToTheOfflineQueuePage()
    {
        _offlineQueueDriver.NavigateToOfflineQueue();
    }

    [Then("the queued item shows a delete button")]
    public void ThenTheQueuedItemShowsADeleteButton()
    {
        _offlineQueueDriver.HasDeleteButtons().Should().BeTrue(
            "each queued item should have a delete action");
    }

    [When("Alex logs out")]
    public void WhenAlexLogsOut()
    {
        _authDriver.LogoutUser();
    }

    [When("user Patricia logs in")]
    public void WhenUserPatriciaLogsIn()
    {
        _authDriver.PreformLoginForUser("patricia@test.com", "Capstone26!");
    }

    [When("Patricia navigates to the offline queue page")]
    public void WhenPatriciaNavigatesToTheOfflineQueuePage()
    {
        _offlineQueueDriver.NavigateToOfflineQueue();
    }

    [Then("Patricia sees no queued items belonging to Alex")]
    public void ThenPatriciaSeesNoQueuedItemsBelongingToAlex()
    {
        // Patricia's IndexedDB store is keyed to her own user ID, so Alex's items are invisible.
        _offlineQueueDriver.HasQueuedItems().Should().BeFalse(
            "Patricia's queue store is keyed to her own user ID and should not contain Alex's items");
    }
}
