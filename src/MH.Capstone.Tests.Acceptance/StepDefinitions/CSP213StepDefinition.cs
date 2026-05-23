using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Seeding;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "profile")]
[ExcludeFromCodeCoverage]
public class CSP213StepDefinitions
{
    private readonly DisplayNameDriver _displayNameDriver;
    private readonly AuthenticationDriver _authenticationDriver;
    private readonly EmailVerificationDriver _emailVerificationDriver;
    private readonly ProfileDriver _profileDriver;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public CSP213StepDefinitions(
        DisplayNameDriver displayNameDriver,
        AuthenticationDriver authenticationDriver,
        EmailVerificationDriver emailVerificationDriver,
        ProfileDriver profileDriver,
        AccountSettingsDriver accountSettingsDriver,
        IWebDriver driver,
        WebDriverWait wait,
        AcceptanceTestSettings settings)
    {
        _displayNameDriver = displayNameDriver;
        _authenticationDriver = authenticationDriver;
        _emailVerificationDriver = emailVerificationDriver;
        _profileDriver = profileDriver;
        _driver = driver;
        _wait = wait;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    // Wipe + re-seed before every @audit scenario so
    //      audit and report counts stay consistent.
    [BeforeScenario("audit")]
    public async Task BeforeProfileScenario()
    {
        await TestWebAppHost.ResetSeedDataAsync();
        _driver.Manage().Cookies.DeleteAllCookies();
    }

    // Scenario 1: Audit log not visible to standard user

    [Given("Alex is logged in")]
    public void GivenAlexIsLoggedIn()
    {
        _authenticationDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("Alex navigates directly to the audit log page URL")]
    public void WhenAlexNavigatesDirectlyToTheAuditLogPageURL()
    {
        
    }

    [Then("Alex receives an access-denied response")]
    public void ThenAlexReceivesAnAccessDeniedResponse()
    {
        
    }

    // Scenario 2: Resolving a report creates an audit entry

    /*

    Scenario: Audit log page is not accessible to regular users
        Given Alex is logged in
        When Alex navigates directly to the audit log page URL
        Then Alex receives an access-denied response
    
    Scenario: Resolving a report creates an audit log entry
        Given an admin is logged in
        And an unresolved report exists
        When the admin resolves the report
        And the admin navigates to the audit log page
        Then an entry is visible for the Report Resolved action
        And the entry shows the admin's display name and a recent timestamp

    Scenario: Locking a user creates an audit log entry
        Given an admin is logged in
        And an active user account exists
        When the admin locks that user account
        And the admin navigates to the audit log page
        Then an entry is visible for the locking action
        And the entry references the locked user

    Scenario: Unlocking a user creates an audit log entry
        Given an admin is logged in
        And a locked user account exists
        When the admin unlocks that user account
        And the admin navigates to the audit log page
        Then an entry is visible for the unlocking action
        And the entry references the unlocked user
    */
}