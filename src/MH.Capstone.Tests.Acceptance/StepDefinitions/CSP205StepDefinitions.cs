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
public class CSP205StepDefinitions
{
    private readonly DisplayNameDriver _displayNameDriver;
    private readonly AuthenticationDriver _authenticationDriver;
    private readonly EmailVerificationDriver _emailVerificationDriver;
    private readonly ProfileDriver _profileDriver;
    private readonly AccountSettingsDriver _accountSettingsDriver;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public CSP205StepDefinitions(
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
        _accountSettingsDriver = accountSettingsDriver;
        _driver = driver;
        _wait = wait;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    // Wipe + re-seed before every @profile scenario so the bio-update scenario doesn't leave
    // a mutated Alex bio behind for later runs, and so Lily's seeded sightings/points are present.
    [BeforeScenario("profile")]
    public async Task BeforeProfileScenario()
    {
        await TestWebAppHost.ResetSeedDataAsync();
        _driver.Manage().Cookies.DeleteAllCookies();
    }

    // -- Scenario 1: Alex views Lily's profile --------------------------------

    [Given("I am looking at Lilys profile page")]
    public void GivenIAmLookingAtLilysProfilePage()
    {
        _authenticationDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
        _profileDriver.NavigateToProfile(AcceptanceTestSeeder.LilyUserId);
    }

    [When("I look at the page details")]
    public void WhenILookAtThePageDetails()
    {
        // The page is fully rendered once the points span is in the DOM — every profile
        // renders it, even for users with zero points (controller falls back to 0).
        _profileDriver.IsPointsValueVisible().Should().BeTrue(
            "the profile page should have loaded with all expected sections");
    }

    [Then("I can see her current point count")]
    public void ThenICanSeeHerCurrentPointCount()
    {
        _profileDriver.IsPointsValueVisible().Should().BeTrue();
        _profileDriver.GetPointsValue().Should().BeGreaterThanOrEqualTo(0,
            "Lily is seeded with a non-negative point total; the value should render as an integer");
    }

    [Then("her recent Clubs")]
    public void ThenHerRecentClubs()
    {
        // The Recent Clubs section is always rendered, whether or not the user is in any
        // clubs — empty state is a span with id="profileRecentClubsEmpty" inside the <li>.
        // The seeder doesn't enroll Lily in any clubs, so we assert presence-of-section
        // rather than presence-of-link.
        _profileDriver.IsRecentClubsSectionPresent().Should().BeTrue(
            "the Recent Clubs list must render on every profile, even when the user is in no clubs");
    }

    [Then("her recent Sightings")]
    public void ThenHerRecentSightings()
    {
        _profileDriver.IsRecentSightingsSectionPresent().Should().BeTrue();
        _profileDriver.GetRecentSightingsCardCount().Should().BeGreaterThan(0,
            "Lily has 5 seeded sightings; at least one card should render on her profile");
    }

    // -- Scenario 2: Alex views their own profile -----------------------------

    [Given("I am looking at my own profile page")]
    public void GivenIAmLookingAtMyOwnProfilePage()
    {
        _authenticationDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
        _profileDriver.NavigateToOwnProfile();
    }

    [When("I read the information provided")]
    public void WhenIReadTheInformationProvided()
    {
        // Sanity-check that the seeded bio is what we start from; the Then step replaces it.
        _profileDriver.GetBioText().Should().Be(
            "Wildlife enthusiast from Monmouth, OR.",
            "Alex's seeded bio should render before the update");
    }

    [Then("I should see it update if I change my bio")]
    public void ThenIShouldSeeItUpdateIfIChangeMyBio()
    {
        // Use a unique value so a leftover bio from a prior run can't make this pass by accident.
        var newBio = $"Updated by acceptance test at {DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ssZ}";

        _accountSettingsDriver.UpdateBio(newBio);
        _profileDriver.NavigateToOwnProfile();

        _profileDriver.GetBioText().Should().Be(newBio,
            "the bio shown on the profile must reflect the value just saved via /dashboard/settings");
    }
}
