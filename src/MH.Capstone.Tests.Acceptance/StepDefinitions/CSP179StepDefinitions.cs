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
[Scope(Feature = "Admin Report System")]
public class CSP179StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AuthenticationDriver _authDriver;
    private readonly string _baseUrl;
    
    // Field to track state between When and Then steps
    private int _initialReportCount;

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
        // Check if we were redirected to an AccessDenied page 
        // OR simply check that the admin-specific header is missing.
        var pageSource = _driver.PageSource;

        bool isAdminPageDisplayed = pageSource.Contains("Admin Report Queue");

        isAdminPageDisplayed.Should().BeFalse("The Admin queue should not be visible to regular users.");

        // Verify we are not on the Reports URL (standard Identity behavior redirects to /Account/AccessDenied)
        _driver.Url.Should().NotContain
            ("/Admin/Reports", "Users should be redirected away from restricted admin queue.");
    }

    // Scenario 2: Admin can view and filter on Report Queue page

    [Given("a moderator is authenticated")]
    public void GivenAModeratorIsAuthenticated()
    {
        _authDriver.PreformLoginForUser("patricia@test.com", "Capstone26!");
    }

    [When("they open the moderation queue and apply filters")]
    public void WhenTheyOpenTheModerationQueueAndApplyFilters()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Admin/Reports");

        // Check/Capture initial state before filtering 
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        _initialReportCount = rows.Count;

        // Apply a filter (search for a specific user) 
        var userSearchInput = _driver.FindElement(By.Id("UserSearch"));
        userSearchInput.Clear();
        userSearchInput.SendKeys("Alex"); // Seeded user in Scenario 1

        // Change the Sort By Descending dropdown
        var sortSelect = new SelectElement(_driver.FindElement(By.Id("SortBy")));
        sortSelect.SelectByValue("Reporter");

        // Click the Filter button (acts as "Submit")
        var filterButton = _driver.FindElement(By.CssSelector("button[type='submit'].btn-dark"));
        filterButton.Click();
    }

    [Then("the queue list is filtered and results are paged")]
    public void ThenTheQueueListIsFilteredAndResultsArePaged()
    {
        // Wait for the table to refresh (ensures report row(s) are present)
        _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);

        var filteredRows = _driver.FindElements(By.CssSelector("table tbody tr"));

        // If the filter worked, the rows should be <= the initial count
        filteredRows.Count.Should().BeLessThanOrEqualTo(_initialReportCount, 
            "The filter should narrow down the results.");

        // Every row's "Reporter" column (2nd <td>) should contain "Alex"
        foreach (var row in filteredRows)
        {
            var reporterName = row.FindElement(By.CssSelector("td:nth-child(2)")).Text;
            reporterName.Should().Contain("Alex", 
                "Filtered results should only show the searched user.");
        }

        // Check for pagination
        var paginationExists = _driver.FindElements(By.CssSelector("ul.pagination")).Any();
        paginationExists.Should().BeTrue("The pagination controls should be visible to the moderator.");
    }

    // Scenario 3: Resolved/Unresolved ticket toggling
    [Given("an Admin clicks the Details button on a report")]
    public void GivenAnAdminClicksTheDetailsButtonOnAReport()
    {
        _authDriver.PreformLoginForUser("patricia@test.com", "Capstone26!");

    }

    [When("the Admin clicks Resolve or Open")]
    public void WhenTheAdminClicksResolveOrOpen()
    {
        
    }

    [When("clicks Confirm on the Details modal")]
    public void WhenClicksConfirmOnTheDetailsModal()
    {
        
    }

    [Then("the selected report is inverted from its previous status")]
    public void ThenTheSelectedReportIsInvertedFromItsPreviousStatus()
    {
        
    }


    /* Current test:
        Scenario: Admin resolves a ticket
            Given an Admin clicks the Details button on a report
            When the Admin clicks Resolve or Open
            And clicks Confirm on the Details modal
            Then the selected report is inverted from its previous status
    */

    /* LAST TEST:
        Scenario: Admin soft-locks a user account
            Given a moderator searches user accounts
            When they toggle a soft-lock on the account
            Then the account is marked as soft-locked and is unable to log in
    */

    /* Test list:
        - Admin report page is locked to admin account logins, return HTTP 403 if
                invalid user tries to access -- DONE (returned 404.)

        - Admin report page displays reports, shows IsResolved value -- DONE
            Can be filtered by IsResolved bool, Reporter (include unresolved reports)
                and by SubmittedAt DateTime (default to UTC for simplicity?)

        - Admin can resolve a ticket.

        - Admin can soft-ban user, creating an appeal entry.
            NOTE: Run EF migration for DateTimeOffset update here, as well as
                "softLocked" boolean value for ApplicationUser.
                    If softLocked == true, lock out of logging in.
                    Mutable field for Admins only.

    */
}