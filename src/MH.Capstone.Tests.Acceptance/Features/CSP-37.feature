@csp37
Feature: Edit Sighting
  As a user, after I have submitted a sighting I want to edit its description and
  species name so that I can correct mistakes or add detail after submission
  without losing my original submission. GPS, timestamp, photo, and scoring stay frozen.

  Scenario: Owner edits their sighting description and species name
    Given user Alex is logged in
    And Alex owns a seeded sighting
    When Alex opens the edit page for that sighting
    And Alex submits species "Gray Wolf" with description "Updated: a wolf at dusk."
    Then Alex is taken to the sighting details page
    And the details page shows species "Gray Wolf"
    And the details page shows description "Updated: a wolf at dusk."

  Scenario: Edit form is pre-populated with current sighting values
    Given user Alex is logged in
    And Alex owns a seeded sighting
    When Alex opens the edit page for that sighting
    Then the species field contains "Great Blue Heron"
    And the description field contains "Great blue heron standing motionless at the WOU campus pond."

  Scenario: Edit button is visible to the sighting owner on the details page
    Given user Alex is logged in
    And Alex owns a seeded sighting
    When Alex views the details page for that sighting
    Then an edit link is visible on the details page

  Scenario: Edit button is not visible to a non-owner on the details page
    Given user Patricia is logged in
    And Alex owns a seeded sighting
    When Patricia views the details page for Alex's sighting
    Then no edit link is visible on the details page

  Scenario: Non-owner cannot access the edit page directly
    Given user Patricia is logged in
    And Alex owns a seeded sighting
    When Patricia navigates directly to the edit page for Alex's sighting
    Then access to the edit page is denied

  Scenario: Unauthenticated user is redirected to login when accessing the edit page
    Given visitor James is signed out
    And Alex owns a seeded sighting
    When James navigates directly to the edit page for that sighting
    Then access to the edit page is denied

  Scenario: Edit form rejects an empty description
    Given user Alex is logged in
    And Alex owns a seeded sighting
    When Alex opens the edit page for that sighting
    And Alex submits species "Coyote" with an empty description
    Then a validation error is shown on the edit form
    And the stored sighting species name is still "Great Blue Heron"

  Scenario: Edit form rejects an empty species name
    Given user Alex is logged in
    And Alex owns a seeded sighting
    When Alex opens the edit page for that sighting
    And Alex submits an empty species with description "Still out here."
    Then a validation error is shown on the edit form
    And the stored sighting species name is still "Great Blue Heron"
