using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Helpers;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
// This isolates the step definition methods used to this feature file only
// THEY WILL FAIL DUE TO DEFINITION AMBIGUITY IF YOU REMOVE THIS!!!
[Scope(Feature = "Admin Report System")]
public class CSP179StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AuthenticationDriver _authDriver;
    private readonly SightingsDriver _sightingsDriver;

    // Holds the path of a temp file for Sighting image uploads.
    private string? _preparedImageFilePath;
    private bool _isBadgeLinkDisplayed;
    private readonly string _baseUrl;

    public CSP179StepDefinitions(
        IWebDriver driver,
        WebDriverWait wait,
        AuthenticationDriver authDriver,
        AcceptanceTestSettings settings)
    {
        _driver = driver;
        _wait = wait;
        _authDriver = authDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    // Resets Alex's sighting count and badge state before each scenario so the
    // full-suite run does not see Sighting Novice already earned from sightings
    // other features submit as Alex (CSP-53, CSP-122, CSP-125, CSP-141,
    // CSP-144, CSP-193, plus this feature's own Scenario 4).
    [BeforeScenario("report")]
    public static async Task BeforeBadgeScenario()
    {
        await TestWebAppHost.ResetSeedDataAsync();
    }

    // Scenario 1: Non-moderator cannot access admin report page

    [Given("a regular authenticated user logs in")]
    public void GivenARegularAuthenticatedUserLogsIn()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("they attempt to access the moderation queue URL")]
    public void WhenTheyAttemptToAccessTheModerationQueueURL()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Admin/Reports");
    }

    [Then("access is denied and no moderation controls are visible")]
    public void ThenAccessIsDeniedAndNoModerationControlsAreVisible()
    {
        bool _isAdminPageDisplayed = true;
        bool _isAdminPageDisplayed.Should().BeFalse("The Admin queue should not be visible to regular users.");
    }

    /*
        Current test:
        Scenario: Non-moderator cannot access moderation tools
            Given a regular authenticated user logs in
            When they attempt to access the moderation queue URL
            Then access is denied and no moderation controls are visible
    */

    /* Next test:
        Scenario: Moderator filters and views queue
            Given a moderator is authenticated
            When they open the moderation queue and apply filters (page, date, reporter)
            Then the queue list is filtered accordingly and results are paginated
    */

    /* Test list:
        - Admin report page is locked to admin account logins, return HTTP 403 if
                invalid user tries to access

        - Admin report page displays reports, shows IsResolved value
            Can be filtered by IsResolved bool, Reporter (include unresolved reports)
                and by SubmittedAt DateTime (default to UTC for simplicity?)

        - 

    */
}