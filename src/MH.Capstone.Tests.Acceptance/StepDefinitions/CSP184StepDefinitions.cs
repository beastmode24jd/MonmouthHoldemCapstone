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
    private readonly string _baseUrl;

    public CSP184StepDefinitions(
        IWebDriver driver,
        WebDriverWait wait,
        AuthenticationDriver authDriver,
        SightingsDriver sightingsDriver,
        AcceptanceTestSettings settings)
    {
        _driver = driver;
        _wait = wait;
        _authDriver = authDriver;
        _sightingsDriver = sightingsDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
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
        _driver.Navigate().GoToUrl($"{_baseUrl}/Dashboard/Badges");
    }

    [Then("the Badge icon should be greyed out")]
    public void ThenTheBadgeIconShouldBeGreyedOut()
    {
        // Find all cards that are specifically marked as 'badge-greyed'
        //      This class is applied to unearned badges
        var greyedBadges = _wait.Until(d => d.FindElements(By.ClassName("badge-greyed")));
        
        // Assert that we found at least one greyed out badge
        greyedBadges.Count.Should().BeGreaterThan(0, "at least one badge should be locked/greyed out");

        // Check for the 'badge-icon-locked' class applied to the image
        var lockedIcon = _driver.FindElement(By.ClassName("badge-icon-locked"));
        lockedIcon.Displayed.Should().BeTrue();
    }
    [Then("give me a hint on how to start earning it")]
    public void ThenGiveMeAHintOnHowToStartEarningIt()
    {
        // Hint is displayed inside a <p> with class 'badge-hint'
        var hint = _wait.Until(d => d.FindElement(By.ClassName("badge-hint")));
        
        hint.Displayed.Should().BeTrue("unearned badges should display a hint");
        hint.Text.Should().Contain("How to earn:", "the hint text should guide the user");
    }

    // Scenario 3: Badge progression and next milestone
    [Given("I have partial progress on a multi step badge")]
    public void GivenIHavePartialProgressOnAMultiStepBadge()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");

        // Alex should have a "First Sighting" Badge.
        // This means progress has started on the "Sighting Student" Badge (25 Sightings submitted)
        // Gallery says Alex has 4 Sightings uploaded.
    }

    [When("I view my Badge page")]
    public void WhenIViewMyBadgePage()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Dashboard/Badges");
    }

    [Then("a progress bar and the countdown remaining is displayed")]
    public void ThenAProgressBarAndTheCountdownRemainingIsDisplayed()
    {
        // BadgeSteps int field in Badge.cs
        // BadgeProgress int field in UserBadge.cs
        
    }

    [Then("a prompt is shown to guide my progress")]
    public void ThenAPromptIsShownToGuideMyProgress()
    {
        // Should show "How to Earn" field.

    }
    /*
        Scenario: Alex sees badge progression and next milestone
            Given I have partial progress on a multi-step badge
            When I view my Badge page
            Then a progress bar and the countdown remaining is displayed
            And a prompt is shown to guide my progress
    */

    /* LAST GHERKIN TEST:
        Scenario: Alex's Badge page processes updates after relevant action
            Given I performs an action that advances Badge progress
            When the website processes my action
            Then the Badge page updates
    */

    /*
        Testing order:
            - Custom Badges page -- DONE
            
            - Add placeholder hints and prompts per-badge -- DONE

            - Add new badge with multi-step progression

            - Automate the check for multi-step progression saving
                - Write and test with NUnit before testing front-end
    */
}