@csp200
Feature: CSP-200 User Search by DisplayName, never Email
    As an Authenticated User,
    I want the user search to match against display names only,
    so that user email addresses are not exposed to other users on the platform.

    Background:
        Given user Alex is logged in for user search
        And I am on the user search page

    Scenario: Searching by an email substring returns no results
        When I enter "@test.com" into the user search bar
        Then I should see the no users found message

    Scenario: Searching by an email local-part returns no results
        When I enter "alex@" into the user search bar
        Then I should see the no users found message

    Scenario: Searching by display name substring returns the matching user
        When I enter "Lily" into the user search bar
        Then I should see at least 1 user search result
        And the user search result at position 1 should display "Lily"

    Scenario: Search results never expose user email addresses
        When I enter "a" into the user search bar
        Then no user search result should contain an "@" character

    Scenario: Display name search is case-insensitive
        When I enter "PATRICIA" into the user search bar
        Then I should see at least 1 user search result
        And the user search result at position 1 should display "Patricia"
