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
    private string? _preparedImageFilePath;
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

        // Navigate to the dashboard to trigger the soft-update in DashboardController.Index(),
        // which writes Alex's Sighting Novice progress (4 sightings seeded) to the DB.
        // Alex should have a "First Sighting" Badge already earned.
        // This means progress has started on the "Sighting Novice" Badge (5 Sightings to complete)
        _driver.Navigate().GoToUrl($"{_baseUrl}/dashboard");
        _wait.Until(d => d.FindElement(By.Id("accountSettingsLink")));
    }

    [When("I view my Badge page")]
    public void WhenIViewMyBadgePage()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Dashboard/Badges");
    }

    [Then("a progress bar and step count is displayed")]
    public void ThenAProgressBarAndStepCountIsDisplayed()
    {
        // Use a more specific selector to ensure we are looking inside a badge card
        // Wait for the progress bar to appear
        var progressBar = _wait.Until(d => 
        {
            var element = d.FindElement(By.ClassName("progress"));
            return element.Displayed ? element : null;
        });

        progressBar.Should().NotBeNull("the progress bar should render for multi-step badges");

        // Check the text for "X / Y" progress
        var stepCountElement = _driver.FindElement(By.CssSelector(".badge-step-count"));
        stepCountElement.Text.Should().MatchRegex(@"\d+ / \d+", "the badge should show current progress vs total steps");
        
    }

    [Then("a prompt is shown to guide my progress")]
    public void ThenAPromptIsShownToGuideMyProgress()
    {
        // Verify "How to earn" field is displayed 
        var hint = _wait.Until(d => d.FindElement(By.ClassName("badge-hint")));
        
        hint.Displayed.Should().BeTrue("unearned badges should display a hint to guide the user");
        hint.Text.Should().Contain("How to earn:", "the hint should provide clear instructions to the user");
    }

    // Scenario 4: Badge awarded when progress cap is hit
    [Given("I perform an action that advances Badge progress")]
    public void GivenIPerformAnActionThatAdvancesBadgeProgress()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");

        // Navigate to the dashboard to trigger the soft-update in DashboardController.Index(),
        // which writes Alex's Sighting Novice progress (4 sightings seeded) to the DB.
        _driver.Navigate().GoToUrl($"{_baseUrl}/dashboard");
        _wait.Until(d => d.FindElement(By.Id("accountSettingsLink")));

        // Submit a Sighting to earn the "Sighting Novice" Badge for Alex.
        // Go to sighting upload page
        _sightingsDriver.NavigateToSightingsUpload();

        var path = TestImageFactory.CreateValid();
        _preparedImageFilePath = path;

        _sightingsDriver.SetImageForUpload(path);

        // Set the other form fields to valid values as well.
        _sightingsDriver.SetDescription("This was generated by acceptance/system tests.");
        _sightingsDriver.SetLatitude(45.0);
        _sightingsDriver.SetLongitude(88.79);
        _sightingsDriver.SetTimestamp(DateTimeOffset.Now);

        // Submit the Sighting
        _sightingsDriver.SubmitSightingsForm();
    }

    [When("the website processes my action")]
    public void WhenTheWebsiteProcessesMyAction()
    {
        // Navigate to Badges page, wait for it to load
        _driver.Navigate().GoToUrl($"{_baseUrl}/Dashboard/Badges");

        _wait.Until(d => d.FindElement(By.Id("currentUserId")));
    }

    [Then("the Badge page updates")]
    public void ThenTheBadgePageUpdates()
    {
        //The Sighting Novice badge is CURRENTLY the only multi-step badge.
        // Now that it is earned, its progress bar should be gone entirely.
        var progressBars = _driver.FindElements(By.ClassName("progress"));
        progressBars.Count.Should().Be(0, "the Sighting Novice badge should now be earned, removing the progress bar");

        // The Sighting Novice card should now show the green 'Earned' indicator.
        // span.badge.bg-success is the element rendered in Badges.cshtml for earned badges.
        var earnedIndicator = _wait.Until(d =>
        {
            var spans = d.FindElements(By.CssSelector("span.badge.bg-success"));
            return spans.FirstOrDefault(s => s.Text.Contains("Earned"));
        });

        earnedIndicator.Should().NotBeNull("the Sighting Novice badge card should display the Earned indicator");
        earnedIndicator!.Displayed.Should().BeTrue("the Earned indicator should be visible on the page)");
    }

    /*
        Testing order:
            - Custom Badges page -- DONE
            
            - Add placeholder hints and prompts per-badge -- DONE

            - Add new badge with multi-step progression -- DONE

            - Automate the check for multi-step progression saving 
    */
}