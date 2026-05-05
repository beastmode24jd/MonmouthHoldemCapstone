using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using MH.Capstone.Tests.Acceptance.Helpers;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP184StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly AuthenticationDriver _authDriver;
    private readonly SightingsDriver _sightingsDriver;

    // Holds the path of a temp file for Sighting image uploads.
    private string? _preparedImageFilePath;

    public CSP184StepDefinitions(
        IWebDriver driver,
        AuthenticationDriver authDriver,
        SightingsDriver sightingsDriver)
    {
        _driver = driver;
        _authDriver = authDriver;
        _sightingsDriver = sightingsDriver;
    }

    // Scenario 1: Dedicated Badges page

    [Given("I am logged in")]
    public void GivenIAmLoggedIn()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("I look at the nav bar")]
    public void WhenILookAtTheNavBar()
    {
        
    }

    [Then("I should see an option for a Badges page")]
    public void ThenIShouldSeeAnOptionForABadgesPage()
    {
        
    }

    /*
        @badge
        Scenario: Alex is logged in and looking at their nav bar
            Given I am logged in
            When I look at the nav bar
            Then I should see an option for a Badges page
    */

    /* NEXT GHERKIN TEST:
        @badge
        Scenario: Alex has no badge progress on a badge
            Given I have no badge progress
            When I view my Badges page
            Then the Badge icon should be greyed out
            And give me a hint on how to start earning it
    */

    /*
        Testing order:
            - Custom Badges page
                - Reroute pre-existing Badge display to this page
            
            - Add placeholder hints and prompts per-badge
                - May need to run EF migration to add field

            - Add new badge with multi-step progression
                - Again, may need to add extra field to EF migration

            - Automate the check for multi-step progression saving
                - Write and test with NUnit before testing front-end
    */
}