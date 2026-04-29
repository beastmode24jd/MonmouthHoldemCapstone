@anidex
Feature: Personal Anidex Collection (CSP-142)
  As an authenticated wildlife AID user
  I want a personal Anidex page that displays every unique species I've confirmed
  So that I can track my collection progress and stay engaged through exploration

  @functionality
  Scenario: User views their populated Anidex
    Given user Alex is logged in
    When Alex navigates to the Anidex page
    Then Alex should see at least one species entry in the Anidex
    And every visible Anidex entry should show a species name and a rarity badge

  @functionality
  Scenario: User views an empty Anidex
    Given user Patricia is logged in
    When Patricia navigates to the Anidex page
    Then the Anidex empty state should be visible

  @functionality
  Scenario: User sees discovery count for a species seen multiple times
    Given user Alex is logged in
    When Alex navigates to the Anidex page
    Then the "Great Blue Heron" Anidex entry should display a discovery count of 2

  @functionality
  Scenario: Anidex only shows the authenticated user's species
    Given user Alex is logged in
    When Alex navigates to the Anidex page
    Then Alex's Anidex should not contain a "Wolverine" entry
