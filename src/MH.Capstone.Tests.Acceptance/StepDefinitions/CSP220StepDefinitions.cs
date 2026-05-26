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
[Scope(Tag = "csp220")]
[ExcludeFromCodeCoverage]
public class CSP220StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AuthenticationDriver _authDriver;
    private readonly SightingsDriver _sightingsDriver;
    private readonly SightingGalleryDriver _galleryDriver;
    private readonly OfflineQueueDriver _offlineQueueDriver;
    private readonly string _baseUrl;

    // Populated during the scenario so AfterScenario can clear IndexedDB.
    private string _userId = string.Empty;
    private string? _tempImagePath;

    public CSP220StepDefinitions(
        IWebDriver driver,
        WebDriverWait wait,
        AuthenticationDriver authDriver,
        SightingsDriver sightingsDriver,
        SightingGalleryDriver galleryDriver,
        OfflineQueueDriver offlineQueueDriver,
        AcceptanceTestSettings settings)
    {
        _driver = driver;
        _wait = wait;
        _authDriver = authDriver;
        _sightingsDriver = sightingsDriver;
        _galleryDriver = galleryDriver;
        _offlineQueueDriver = offlineQueueDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    // ── Hooks ────────────────────────────────────────────────────────────────

    [BeforeScenario("csp220")]
    public void BeforeCsp220Scenario()
    {
        _driver.Manage().Cookies.DeleteAllCookies();
    }

    // Runs for the sync scenario only — wipes and re-seeds the DB so Alex starts
    // with his fixed set of sightings and the gallery assertion is deterministic.
    [BeforeScenario("csp220-sync")]
    public static async Task BeforeCsp220SyncScenario()
    {
        await TestWebAppHost.ResetSeedDataAsync();
    }

    [AfterScenario("csp220")]
    public void AfterCsp220Scenario()
    {
        _offlineQueueDriver.SimulateOnline();

        // Best-effort: clear IndexedDB so leftover offline items don't bleed into
        // later scenarios. Requires a page that loads offline-queue.js.
        if (!string.IsNullOrEmpty(_userId))
        {
            try
            {
                _offlineQueueDriver.NavigateToOfflineQueue();
                _offlineQueueDriver.ClearIndexedDb(_userId);
            }
            catch { /* non-fatal */ }
        }

        // Clean up any temp image files created during the scenario.
        if (_tempImagePath != null && File.Exists(_tempImagePath))
        {
            File.Delete(_tempImagePath);
            _tempImagePath = null;
        }
    }

    // ── Scenario 1: Pre-cache ─────────────────────────────────────────────

    [When("the service worker becomes active")]
    public void WhenTheServiceWorkerBecomesActive()
    {
        // Allow the pre-cache script in _Layout.cshtml time to fire.
        Thread.Sleep(3000);

        _offlineQueueDriver.WaitForServiceWorkerReady()
            .Should().BeTrue("the service worker should register and activate after page load");
    }

    [Then("the home page is cached for offline use")]
    public void ThenTheHomePageIsCachedForOfflineUse()
    {
        _offlineQueueDriver.IsCachedByServiceWorker($"{_baseUrl}/")
            .Should().BeTrue("the pre-cache script should store the home page in cwsa-offline-v1 on login");
    }

    [Then("the sighting create page is cached for offline use")]
    public void ThenTheSightingCreatePageIsCachedForOfflineUse()
    {
        _offlineQueueDriver.IsCachedByServiceWorker($"{_baseUrl}/Sighting/Create")
            .Should().BeTrue("the pre-cache script should store /Sighting/Create in cwsa-offline-v1 on login");
    }

    [Then("the offline queue page is cached for offline use")]
    public void ThenTheOfflineQueuePageIsCachedForOfflineUse()
    {
        _offlineQueueDriver.IsCachedByServiceWorker($"{_baseUrl}/Sighting/OfflineQueue")
            .Should().BeTrue("the pre-cache script should store /Sighting/OfflineQueue in cwsa-offline-v1 on login");
    }

    // ── Scenario 2: Offline save to IndexedDB ────────────────────────────

    [When("Alex navigates to the sighting upload page")]
    public void WhenAlexNavigatesToTheSightingUploadPage()
    {
        _sightingsDriver.NavigateToSightingsUpload();

        // Capture userId now — the Upload page has the currentUserId element since CSP-220 fix.
        _userId = _offlineQueueDriver.GetCurrentUserIdFromPage();
    }

    [When("the device is simulated as offline")]
    public void WhenTheDeviceIsSimulatedAsOffline()
    {
        _offlineQueueDriver.SimulateOffline();
    }

    [When("Alex fills in the sighting form")]
    public void WhenAlexFillsInTheSightingForm()
    {
        // CSP-189: the upload form rejects Low-tier photos, so use the high-quality
        // stripes preset so the form accepts the upload and the offline-queue flow runs.
        _tempImagePath = TestImageFactory.CreateValid();

        _sightingsDriver.SetImageForUpload(_tempImagePath);
        _sightingsDriver.SetSpeciesName("CSP220-OfflineTestSpecies");
        _sightingsDriver.SetLatitude(45.0);
        _sightingsDriver.SetLongitude(-123.0);
        _sightingsDriver.SetTimestamp(DateTimeOffset.Now.AddMinutes(-1));
        _sightingsDriver.SetDescription("CSP-220 offline queue acceptance test.");
    }

    [When("Alex submits the sighting form")]
    public void WhenAlexSubmitsTheSightingForm()
    {
        // Must use JS click (not form.submit()) so the JS submit event handler fires.
        _offlineQueueDriver.ClickSubmitButton();
    }

    [Then("Alex is redirected to the offline queue page")]
    public void ThenAlexIsRedirectedToTheOfflineQueuePage()
    {
        _offlineQueueDriver.IsOnOfflineQueuePage().Should().BeTrue(
            "submitting while offline should redirect to /Sighting/OfflineQueue");
    }

    [Then("the offline queue shows at least one item with status pending")]
    public void ThenTheOfflineQueueShowsAtLeastOneItemWithStatusPending()
    {
        // UI check — at least one card rendered.
        _offlineQueueDriver.HasQueuedItems().Should().BeTrue(
            "the OfflineQueue page should render a card for the sighting saved to IndexedDB");

        // IndexedDB check — the item has status 'pending'.
        // Refresh userId from the queue page's element if it wasn't captured on the upload page.
        if (string.IsNullOrEmpty(_userId))
            _userId = _offlineQueueDriver.GetCurrentUserIdFromPage();

        var statuses = _offlineQueueDriver.GetIndexedDbItemStatuses(_userId);
        statuses.Should().NotBeEmpty("at least one sighting should be in IndexedDB");
        statuses.Should().Contain("pending",
            "a newly saved offline sighting should have status 'pending' until synced");
    }

    // ── Scenario 3: Online sync ───────────────────────────────────────────

    [Given("Alex has submitted a sighting while offline")]
    public void GivenAlexHasSubmittedASightingWhileOffline()
    {
        // Navigate to upload, force offline, fill form, submit — same flow as scenario 2.
        _sightingsDriver.NavigateToSightingsUpload();
        _userId = _offlineQueueDriver.GetCurrentUserIdFromPage();

        _offlineQueueDriver.SimulateOffline();

        _tempImagePath = TestImageFactory.CreateValid();
        _sightingsDriver.SetImageForUpload(_tempImagePath);
        _sightingsDriver.SetSpeciesName("CSP220-SyncTestSpecies");
        _sightingsDriver.SetLatitude(45.0);
        _sightingsDriver.SetLongitude(-123.0);
        _sightingsDriver.SetTimestamp(DateTimeOffset.Now.AddMinutes(-1));
        _sightingsDriver.SetDescription("CSP-220 sync test sighting.");

        _offlineQueueDriver.ClickSubmitButton();

        // Wait until we are on the offline queue page with a pending item.
        _wait.Until(d => _offlineQueueDriver.IsOnOfflineQueuePage());

        if (string.IsNullOrEmpty(_userId))
            _userId = _offlineQueueDriver.GetCurrentUserIdFromPage();

        _offlineQueueDriver.GetIndexedDbItemCount(_userId).Should().BeGreaterThan(0,
            "the offline sighting must be in IndexedDB before we test the sync");
    }

    [When("Alex's device comes back online")]
    public void WhenAlexsDeviceComesBackOnline()
    {
        // SimulateOnline fires window.__FORCE_OFFLINE = false and dispatches the 'online'
        // event, which triggers syncOfflineQueue in offline-queue.js.
        _offlineQueueDriver.SimulateOnline();
    }

    [Then("the queued sighting status changes to synced")]
    public void ThenTheQueuedSightingStatusChangesToSynced()
    {
        var synced = _offlineQueueDriver.WaitForQueuedItemToSync(_userId, timeoutSeconds: 20);

        synced.Should().BeTrue("the sighting should be uploaded to the server and marked synced in IndexedDB");

        var statuses = _offlineQueueDriver.GetIndexedDbItemStatuses(_userId);
        statuses.Should().NotContain("failed",
            "the sync should succeed; a 'failed' status means the upload POST was rejected");
    }

    [Then("the synced sighting appears in the sighting gallery")]
    public void ThenTheSyncedSightingAppearsInTheSightingGallery()
    {
        _galleryDriver.NavigateToGallery();
        _galleryDriver.ClickMyFilter();

        _galleryDriver.GetVisibleSightingCount().Should().BeGreaterThan(0,
            "after syncing, at least the uploaded sighting should appear under 'My Sightings'");
    }
}
