Feature: CSP-170 Replace Public Email Attribution with Display Name Site-Wide

Scenario: Sightings gallery shows display name instead of email
    Given user Alex is logged in
    When the user navigates to the gallery page
    Then each sighting attribution shows a display name and not an email address

Scenario: Leaderboard shows display name instead of email
    Given user Alex is logged in
    When I view the leaderboard
    Then each leaderboard entry shows a display name and not an email address
