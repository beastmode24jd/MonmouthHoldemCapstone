using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP172StepDefinitions
{
    // Stable seeded sighting that will be added to AcceptanceTestSeeder for CSP-172:
    // Alex's 4th sighting with SpeciesName = "Mystery Critter Z" — guaranteed not to
    // resolve against the Animals API, so the details page must take the fallback path.
    private static readonly Guid AlexUnresolvedSpeciesSightingId =
        new("a1000000-0000-0000-0000-000000000004");

    // Any sighting ID that does not exist in the seed set.
    private static readonly Guid NonexistentSightingId =
        new("99999999-9999-9999-9999-999999999999");

    // A known seeded sighting (Alex's Great Blue Heron) — used for the anonymous-redirect
    // scenario where the route just needs to be valid; auth filter is what's under test.
    private static readonly Guid KnownSeededSightingId =
        new("a1000000-0000-0000-0000-000000000001");

    private readonly IWebDriver _driver;
    private readonly AuthenticationDriver _authDriver;
    private readonly SightingDetailsDriver _detailsDriver;

    public CSP172StepDefinitions(
        IWebDriver driver,
        AuthenticationDriver authDriver,
        SightingDetailsDriver detailsDriver)
    {
        _driver = driver;
        _authDriver = authDriver;
        _detailsDriver = detailsDriver;
    }

    [Given("user Lily is logged in")]
    public void GivenUserLilyIsLoggedIn()
    {
        _authDriver.PreformLoginForUser("lily@test.com", "Capstone26!");
    }

    [Given("visitor James is signed out")]
    public void GivenVisitorJamesIsSignedOut()
    {
        _authDriver.LogoutUser();
    }

    [When("the user clicks the first sighting card")]
    public void WhenTheUserClicksTheFirstSightingCard()
    {
        _detailsDriver.ClickFirstSightingCardLink();
    }

    [Then("the sighting details page is displayed")]
    public void ThenTheSightingDetailsPageIsDisplayed()
    {
        _detailsDriver.IsOnDetailsPage().Should().BeTrue(
            "the user should land on /Sighting/Details/{id} after clicking a card");
    }

    [Then("the details page shows the full image, uploader display name, profile icon, full description, timestamp, location, species name, and a fun fact about the animal")]
    public void ThenTheDetailsPageShowsAllSightingInformation()
    {
        var page = new SightingDetailsPageObject(_driver);
        page.Image.Displayed.Should().BeTrue("the full sighting image should be visible");
        page.UploaderName.Text.Trim().Should().NotBeNullOrWhiteSpace("uploader display name should be present");
        page.UploaderName.Text.Should().NotContain("@", "uploader name must be a display name, not an email");
        page.UploaderIcon.Displayed.Should().BeTrue("uploader profile icon should be visible");
        page.Description.Text.Trim().Should().NotBeNullOrWhiteSpace("description text should be visible");
        page.Timestamp.Text.Trim().Should().NotBeNullOrWhiteSpace("timestamp should be rendered");
        page.Location.Text.Trim().Should().NotBeNullOrWhiteSpace("location should be rendered");
        page.Species.Text.Trim().Should().NotBeNullOrWhiteSpace("species name should be rendered");
        page.FunFact.Text.Trim().Should().NotBeNullOrWhiteSpace("a fun fact should be rendered");
    }

    [When("the user opens the sighting details page for a sighting with an unresolved species")]
    public void WhenTheUserOpensSightingWithUnresolvedSpecies()
    {
        _detailsDriver.NavigateToDetails(AlexUnresolvedSpeciesSightingId);
    }

    [Then("the details page still shows the image, uploader display name, profile icon, description, timestamp, and location")]
    public void ThenTheDetailsPageStillShowsCoreInformation()
    {
        var page = new SightingDetailsPageObject(_driver);
        page.Image.Displayed.Should().BeTrue("the sighting image should still render when the fun-fact lookup fails");
        page.UploaderName.Text.Trim().Should().NotBeNullOrWhiteSpace();
        page.UploaderIcon.Displayed.Should().BeTrue();
        page.Description.Displayed.Should().BeTrue();
        page.Timestamp.Text.Trim().Should().NotBeNullOrWhiteSpace();
        page.Location.Text.Trim().Should().NotBeNullOrWhiteSpace();
    }

    [Then("a friendly fallback message is shown in place of the fun fact")]
    public void ThenAFriendlyFallbackMessageIsShown()
    {
        var page = new SightingDetailsPageObject(_driver);
        page.FunFact.Text.Trim().Should().NotBeNullOrWhiteSpace(
            "the fun-fact section should always render — fallback text when the lookup fails");
        page.FunFact.GetAttribute("data-fun-fact-status")
            .Should().Be("fallback",
                "the fallback path should mark the fun-fact element with data-fun-fact-status='fallback'");
    }

    [When("the user navigates directly to a sighting details URL")]
    public void WhenTheUserNavigatesDirectlyToASightingDetailsUrl()
    {
        _detailsDriver.NavigateToDetails(KnownSeededSightingId);
    }

    [Then("the user is redirected to the Login page")]
    public void ThenTheUserIsRedirectedToTheLoginPage()
    {
        _authDriver.WasPageAccessDenied().Should().BeTrue(
            "an unauthenticated visitor should be sent to /Account/Login");
    }

    [When("the user navigates to a sighting details URL that does not match any sighting")]
    public void WhenTheUserNavigatesToANonexistentSightingDetailsUrl()
    {
        _detailsDriver.NavigateToDetails(NonexistentSightingId);
    }

    [Then("the sighting not found page is displayed")]
    public void ThenTheSightingNotFoundPageIsDisplayed()
    {
        _detailsDriver.IsOnNotFoundPage().Should().BeTrue(
            "an unknown sighting ID should render the styled not-found view, not a stack trace");
    }

    [Then("a link back to the Sighting Gallery is visible")]
    public void ThenALinkBackToTheGalleryIsVisible()
    {
        var page = new SightingDetailsPageObject(_driver);
        page.NotFoundBackLinks.Should().NotBeEmpty(
            "the not-found page should offer a way back to the gallery");
        page.NotFoundBackLinks.First().Displayed.Should().BeTrue();
    }
}
