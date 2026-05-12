// Sprint 6, CSP-200: Search by DisplayName, never email.
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP200StepDefinitions
{
    private readonly UserSearchDriver _userSearchDriver;

    public CSP200StepDefinitions(UserSearchDriver userSearchDriver)
    {
        _userSearchDriver = userSearchDriver;
    }

    [Then("I should see at least {int} user search result")]
    [Then("I should see at least {int} user search results")]
    public void ThenIShouldSeeAtLeastNUserSearchResults(int min)
    {
        _userSearchDriver.GetResultCount()
            .Should().BeGreaterThanOrEqualTo(min,
                $"the search should return at least {min} match(es)");
    }

    [Then("the user search result at position {int} should display {string}")]
    public void ThenTheUserSearchResultAtPositionShouldDisplay(int position, string expected)
    {
        var index = position - 1;
        _userSearchDriver.GetResultUsernameAt(index)
            .Should().Be(expected,
                $"result #{position} should display the matched user's DisplayName");
    }

    [Then("no user search result should contain an {string} character")]
    public void ThenNoUserSearchResultShouldContainCharacter(string disallowed)
    {
        var count = _userSearchDriver.GetResultCount();
        count.Should().BeGreaterThan(0, "this scenario expects at least one match to validate against");

        for (var i = 0; i < count; i++)
        {
            _userSearchDriver.GetResultUsernameAt(i)
                .Should().NotContain(disallowed,
                    $"result #{i + 1} must not surface email content (no '{disallowed}')");
        }
    }
}
