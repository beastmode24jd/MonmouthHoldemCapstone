@csp202
Feature: Anidex Card Expansion (CSP-202)
  As a user collecting wildlife sightings, when I open my Anidex and tap a
  species card, I want to see every sighting I have logged under that species
  (with each photo and short description), so the Anidex feels like a per-species
  log instead of only showing the most recent sighting.

  Scenario: User opens a species card with multiple sightings
    Given user Alex is logged in
    When Alex navigates to the Anidex page
    And Alex clicks the "Great Blue Heron" species card
    Then the "Great Blue Heron" sightings dialog is shown
    And the "Great Blue Heron" sightings dialog lists 2 entries

  Scenario: User closes an open species dialog and the grid is untouched
    Given user Alex is logged in
    When Alex navigates to the Anidex page
    And Alex clicks the "Great Blue Heron" species card
    And Alex closes the sightings dialog
    Then the "Great Blue Heron" sightings dialog is not shown

  Scenario: Species with only one sighting does not open a dialog
    Given user Alex is logged in
    When Alex navigates to the Anidex page
    Then the "Bald Eagle" card does not open a sightings dialog
