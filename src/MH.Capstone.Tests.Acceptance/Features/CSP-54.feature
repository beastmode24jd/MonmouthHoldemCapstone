Feature: CSP-54 User Search
    As an Authenticated User,
    I want to be able to search for other users on the platform
    so I can find fellow users on the system.

    Background:
        Given user Alex is logged in for user search
        And I am on the user search page

    Scenario: Search that matches no users shows no-results message and disabled paging
        When I enter "xyzzy_no_match_999" into the user search bar
        Then I should see the no users found message
        And the user search previous arrow should be disabled
        And the user search next arrow should be disabled
        And the user search paging text should be "Page 1 of 1 | Showing 0 Results"

    Scenario: Clicking a search result navigates to that user's profile page
        When I enter "alex" into the user search bar
        And I click on the first user search result
        Then I should be redirected to a user profile page

    # Sprint 6, CSP-200: query was "@test.com" (email substring) — switched to "a"
    # because search now filters by DisplayName only. "a" matches Alex and Patricia.
    Scenario: Search with 10 or fewer results shows all results with paging disabled
        When I enter "a" into the user search bar
        Then I should see user search results below the search form
        And the user search previous arrow should be disabled
        And the user search next arrow should be disabled
        And the user search paging text should start with "Page 1 of 1 | Showing 1-"

    Scenario: Multiple results are sorted in ascending alphabetical order
        When I enter "a" into the user search bar
        Then the user search results should be sorted alphabetically ascending

    @ignore
    Scenario: Search with more than 10 results shows first 10 with paging enabled
        When I enter "@" into the user search bar
        Then I should see user search results below the search form
        And the user search next arrow should be enabled
        And the user search paging text should start with "Page 1 of"
