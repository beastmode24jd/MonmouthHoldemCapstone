using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP96StepDefinitions
{
    private readonly SightingGalleryDriver _galleryDriver;
    private readonly AuthenticationDriver _authDriver;

    public CSP96StepDefinitions(SightingGalleryDriver galleryDriver, AuthenticationDriver authDriver)
    {
        _galleryDriver = galleryDriver;
        _authDriver = authDriver;
    }

    [Given("user alpha is logged in")]
    public void GivenUserAlphaIsLoggedIn()
    {
        _authDriver.PreformLoginForUser("alpha@test.com", "Capstone26!");
    }

    [Given("user newuser is logged in")]
    public void GivenUserNewuserIsLoggedIn()
    {
        _authDriver.PreformLoginForUser("newuser@test.com", "Capstone26!");
    }

    [When("the user navigates to the gallery page")]
    [Given("the user navigates to the gallery page")]
    public void WhenTheUserNavigatesToTheGalleryPage()
    {
        _galleryDriver.NavigateToGallery();
    }

    [Then("sightings from multiple users are displayed")]
    public void ThenSightingsFromMultipleUsersAreDisplayed()
    {
        var usernames = _galleryDriver.GetVisibleAttributionUsernames();
        usernames.Count.Should().BeGreaterThan(1,
            "the community gallery should show sightings from more than one user");
    }

    [Then("each sighting card shows the submitting user attribution")]
    public void ThenEachSightingCardShowsTheSubmittingUserAttribution()
    {
        var count = _galleryDriver.GetVisibleSightingCount();
        count.Should().BeGreaterThan(0, "there should be sightings in the community gallery");

        var usernames = _galleryDriver.GetVisibleAttributionUsernames();
        usernames.Should().NotBeEmpty("each visible card should carry an attribution label");
        usernames.Should().NotContain("Unknown",
            "attribution should display a real username, not the fallback value");
    }

    [When("the user selects the \"My Sightings\" filter")]
    [Given("the user selects the \"My Sightings\" filter")]
    public void WhenTheUserSelectsMyFilter()
    {
        _galleryDriver.ClickMyFilter();
    }

    [Then("only sightings submitted by the authenticated user are displayed")]
    public void ThenOnlyAuthenticatedUserSightingsAreDisplayed()
    {
        // After filtering, all visible attributions must belong to the same user (alpha)
        var usernames = _galleryDriver.GetVisibleAttributionUsernames();
        usernames.Count.Should().BeLessThanOrEqualTo(1,
            "My Sightings filter should show cards from only one user");
    }

    [Then("the My Sightings filter button is active")]
    public void ThenMyFilterButtonIsActive()
    {
        _galleryDriver.IsMyFilterActive().Should().BeTrue("the My Sightings button should be highlighted as active");
    }

    [When("the user selects the \"All Sightings\" filter")]
    public void WhenTheUserSelectsAllFilter()
    {
        _galleryDriver.ClickAllSightingsFilter();
    }

    [Then("the All Sightings filter button is active")]
    public void ThenAllFilterButtonIsActive()
    {
        _galleryDriver.IsAllSightingsFilterActive().Should().BeTrue("the All Sightings button should be highlighted as active");
    }

    [Then("an empty state message is displayed")]
    public void ThenAnEmptyStateMessageIsDisplayed()
    {
        _galleryDriver.IsMyEmptyStateVisible().Should().BeTrue(
            "the My Sightings empty state should be visible when the user has no sightings");
    }

    [Then("the user is prompted to upload their first sighting")]
    public void ThenUserIsPromptedToUpload()
    {
        _galleryDriver.HasUploadLink().Should().BeTrue(
            "the empty state should contain a link to the sighting upload page");
    }

    [When("the user navigates away from the gallery and returns")]
    public void WhenUserNavigatesAwayAndReturns()
    {
        _galleryDriver.NavigateAwayAndReturn();
    }

    [Then("the My Sightings filter is still active")]
    public void ThenMyFilterIsStillActive()
    {
        _galleryDriver.IsMyFilterActive().Should().BeTrue(
            "the My Sightings filter should persist via sessionStorage across gallery page loads");
    }
}
