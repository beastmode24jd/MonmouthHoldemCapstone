using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP58StepDefinitions
{
    private readonly WildlifeSearchDriver _searchDriver;
    private readonly AuthenticationDriver _authenticationDriver;

    public CSP58StepDefinitions(
        WildlifeSearchDriver searchDriver,
        AuthenticationDriver authenticationDriver)
    {
        _searchDriver        = searchDriver;
        _authenticationDriver = authenticationDriver;
    }

    // ── Scenario 1: Species search page displays its expected UI elements ──────

    [When("user navigates to the species search page")]
    public void WhenUserNavigatesToTheSpeciesSearchPage()
    {
        _searchDriver.NavigateToSearchPage();
    }

    [Then("the search input field is visible")]
    public void ThenTheSearchInputFieldIsVisible()
    {
        _searchDriver.IsSearchInputVisible().Should().BeTrue();
    }

    [Then("the search button is visible")]
    public void ThenTheSearchButtonIsVisible()
    {
        _searchDriver.IsSearchButtonVisible().Should().BeTrue();
    }

    [Then("the clear button is visible")]
    public void ThenTheClearButtonIsVisible()
    {
        _searchDriver.IsClearButtonVisible().Should().BeTrue();
    }

    // ── Scenario 2: Searching by a known animal name displays a result card ───

    [Given("user is on the species search page")]
    public void GivenUserIsOnTheSpeciesSearchPage()
    {
        _searchDriver.NavigateToSearchPage();
    }

    [When("user searches for {string}")]
    public void WhenUserSearchesFor(string animalName)
    {
        _searchDriver.SearchFor(animalName);
    }

    [Then("a result card is displayed with an animal name")]
    public void ThenAResultCardIsDisplayedWithAnAnimalName()
    {
        _searchDriver.HasResultWithAnimalName().Should().BeTrue(
            because: "searching for a known species should return at least one result from the Ninjas API");
    }

    // ── Scenario 3: Unrecognised search term shows a polite no-results message ─

    [Then("user sees a polite no-results message")]
    public void ThenUserSeesAPoliteNoResultsMessage()
    {
        _searchDriver.HasNoResultsMessage().Should().BeTrue(
            because: "a search that yields no API matches must display a no-results message to the user");
    }
}
