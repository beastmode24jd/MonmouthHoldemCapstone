// Sprint 5, CSP-54
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP54StepDefinitions
{
    private readonly UserSearchDriver _userSearchDriver;
    private readonly AuthenticationDriver _authDriver;

    public CSP54StepDefinitions(UserSearchDriver userSearchDriver, AuthenticationDriver authDriver)
    {
        _userSearchDriver = userSearchDriver;
        _authDriver = authDriver;
    }

    [Given("user Alex is logged in for user search")]
    public void GivenUserAlexIsLoggedInForUserSearch()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [Given("I am on the user search page")]
    public void GivenIAmOnTheUserSearchPage()
    {
        _userSearchDriver.NavigateToSearch();
    }

    [When("I enter {string} into the user search bar")]
    public void WhenIEnterIntoTheUserSearchBar(string query)
    {
        _userSearchDriver.EnterSearchQuery(query);
    }

    [Then("I should see the no users found message")]
    public void ThenIShouldSeeTheNoUsersFoundMessage()
    {
        _userSearchDriver.IsNoResultsMessageVisible()
            .Should().BeTrue("a search with no matches should display a no-results message");
    }

    [Then("the user search previous arrow should be disabled")]
    public void ThenTheUserSearchPreviousArrowShouldBeDisabled()
    {
        _userSearchDriver.IsPrevButtonDisabled()
            .Should().BeTrue("the previous page button should be disabled when there is nothing to page back to");
    }

    [Then("the user search next arrow should be disabled")]
    public void ThenTheUserSearchNextArrowShouldBeDisabled()
    {
        _userSearchDriver.IsNextButtonDisabled()
            .Should().BeTrue("the next page button should be disabled when there is only one page");
    }

    [Then("the user search previous arrow should be enabled")]
    public void ThenTheUserSearchPreviousArrowShouldBeEnabled()
    {
        _userSearchDriver.IsPrevButtonEnabled()
            .Should().BeTrue("the previous page button should be enabled when there are previous pages");
    }

    [Then("the user search next arrow should be enabled")]
    public void ThenTheUserSearchNextArrowShouldBeEnabled()
    {
        _userSearchDriver.IsNextButtonEnabled()
            .Should().BeTrue("the next page button should be enabled when there are more pages");
    }

    [Then("the user search paging text should be {string}")]
    public void ThenTheUserSearchPagingTextShouldBe(string expectedText)
    {
        _userSearchDriver.GetPagingInfoText()
            .Should().Be(expectedText, $"paging info text should exactly match '{expectedText}'");
    }

    [Then("the user search paging text should start with {string}")]
    public void ThenTheUserSearchPagingTextShouldStartWith(string prefix)
    {
        _userSearchDriver.GetPagingInfoText()
            .Should().StartWith(prefix, $"paging info text should begin with '{prefix}'");
    }

    [When("I click on the first user search result")]
    public void WhenIClickOnTheFirstUserSearchResult()
    {
        _userSearchDriver.GetResultCount()
            .Should().BeGreaterThan(0, "there must be at least one result to click");
        _userSearchDriver.ClickResultAt(0);
    }

    [Then("I should be redirected to a user profile page")]
    public void ThenIShouldBeRedirectedToAUserProfilePage()
    {
        _userSearchDriver.GetCurrentUrl()
            .Should().Contain("/account/", "clicking a result should navigate to the user's account profile page");
    }

    [Then("I should see user search results below the search form")]
    public void ThenIShouldSeeUserSearchResultsBelowTheSearchForm()
    {
        _userSearchDriver.GetResultCount()
            .Should().BeGreaterThan(0, "at least one result should be visible after a matching search");
    }

    [Then("the user search results should be sorted alphabetically ascending")]
    public void ThenTheUserSearchResultsShouldBeSortedAlphabeticallyAscending()
    {
        var count = _userSearchDriver.GetResultCount();
        count.Should().BeGreaterThan(1, "there must be more than one result to verify ordering");

        var usernames = Enumerable.Range(0, count)
            .Select(i => _userSearchDriver.GetResultUsernameAt(i))
            .ToList();

        var sorted = usernames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        usernames.Should().Equal(sorted, "results should be displayed in ascending alphabetical order");
    }
}
