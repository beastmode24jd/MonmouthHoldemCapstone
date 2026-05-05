using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;
using MH.Capstone.Tests.Acceptance.Helpers;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
// This isolates the step definition methods used to this feature file only
// THEY WILL FAIL DUE TO DEFINITION AMBIGUITY IF YOU REMOVE THIS!!!
[Scope(Feature = "Badge Refinement")]
public class CSP184StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AuthenticationDriver _authDriver;
    private readonly SightingsDriver _sightingsDriver;

    // Holds the path of a temp file for Sighting image uploads.
    //private string? _preparedImageFilePath;
    private bool _isBadgeLinkDisplayed;

    public CSP184StepDefinitions(
        IWebDriver driver,
        WebDriverWait wait,
        AuthenticationDriver authDriver,
        SightingsDriver sightingsDriver)
    {
        _driver = driver;
        _wait = wait;
        _authDriver = authDriver;
        _sightingsDriver = sightingsDriver;
    }

    // Scenario 1: Dedicated Badges page

    [Given("Alex is logged in")]
    public void GivenAlexIsLoggedIn()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("Alex looks at their nav bar")]
    public void WhenAlexLooksAtTheirNavBar()
    {
        // Click on the dropdown menu
        var dropdown = _wait.Until(d => d.FindElement(By.Id("userDropdown")));
        dropdown.Click();

        // Wait until badgeMenuItem is visible to the user
        // This lambda returns the element only if .Displayed is true
        var badgeLink = _wait.Until(d => 
        {
            var element = d.FindElement(By.Id("badgeMenuItem"));
            return element.Displayed ? element : null;
        });

        // Save context between steps
        _isBadgeLinkDisplayed = badgeLink.Displayed;
    }

    [Then("Alex should see an option for a Badges page")]
    public void ThenIShouldSeeAnOptionForABadgesPage()
    {
        _isBadgeLinkDisplayed.Should().BeTrue("The Badges page should be visible in the dropdown menu.");
    }

    // Scenario 2: Placeholder hint and changed icon if Badge not earned

    [Given("I have no badge progress")]
    public void GivenIHaveNoBadgeProgress()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("I view my Badges page")]
    public void WhenIViewMyBadgesPage()
    {
        
    }

    [Then("the Badge icon should be greyed out")]
    public void ThenTheBadgeIconShouldBeGreyedOut()
    {
        
    }
    [Then("give me a hint on how to start earning it")]
    public void ThenGiveMeAHintOnHowToStartEarningIt()
    {
        
    }

    /*
        Scenario: Alex has no badge progress on a badge
            Given I have no badge progress
            When I view my Badges page
            Then the Badge icon should be greyed out
            And give me a hint on how to start earning it
    */

    /* NEXT GHERKIN TEST:
        Scenario: Alex sees badge progression and next milestone
            Given I have partial progress on a multi-step badge
            When I view my Badge page
            Then a progress bar and the countdown remaining is displayed
            And a prompt is shown to guide my progress
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