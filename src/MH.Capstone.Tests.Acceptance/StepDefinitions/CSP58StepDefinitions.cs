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
}
