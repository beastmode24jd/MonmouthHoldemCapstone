Feature: CSP-96 Sightings Gallery Community View

Scenario: Default gallery shows all community sightings
    Given user Alex is logged in
    When the user navigates to the gallery page
    Then sightings from multiple users are displayed
    And each sighting card shows the submitting user attribution

Scenario: User filters gallery to their own sightings
    Given user Alex is logged in
    And the user navigates to the gallery page
    When the user selects the "My Sightings" filter
    Then only sightings submitted by the authenticated user are displayed
    And the My Sightings filter button is active

Scenario: User clears the filter to return to community view
    Given user Alex is logged in
    And the user navigates to the gallery page
    And the user selects the "My Sightings" filter
    When the user selects the "All Sightings" filter
    Then sightings from multiple users are displayed
    And the All Sightings filter button is active

Scenario: Empty state when user has no sightings
    Given user Patricia is logged in
    And the user navigates to the gallery page
    When the user selects the "My Sightings" filter
    Then an empty state message is displayed
    And the user is prompted to upload their first sighting

Scenario: Filter persists within the session
    Given user Alex is logged in
    And the user navigates to the gallery page
    And the user selects the "My Sightings" filter
    When the user navigates away from the gallery and returns
    Then the My Sightings filter is still active
