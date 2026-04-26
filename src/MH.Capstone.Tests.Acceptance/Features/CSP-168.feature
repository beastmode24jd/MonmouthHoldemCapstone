Feature: CSP-168 — Set Display Name from Account Settings
  As a User, when I register a new account or log in for the first time after a site update,
  I want to set a display name so that my chosen name appears across the site instead of my email address.

  Background:
    Given the user is not logged in

  Scenario: Display name is required at registration
    When an anonymous user submits the registration form without a display name
    Then an inline validation error is shown for the display name field
    And the account is not created

  Scenario: User sets a display name during registration
    Given a new user registers with display name "Nature Explorer" and a unique test email
    When the user verifies their email
    And the user logs in with their registered credentials
    Then the user is not redirected to the Set Your Display Name page
    And "Nature Explorer" appears as the user's display name on the dashboard

  Scenario: Existing user with UNSET display name is forced to set one at login
    Given the user "faye@test.com" has a display name of "UNSET"
    When "faye@test.com" logs in with password "Capstone26!"
    Then the user is redirected to the Set Your Display Name page

  Scenario: Existing user completes the forced display name setup
    Given the user "owen@test.com" has a display name of "UNSET"
    When "owen@test.com" logs in with password "Capstone26!"
    And the user sets their display name to "Owen Naturalist"
    Then the user is redirected to the dashboard
    And "Owen Naturalist" appears as the user's display name on the dashboard

  Scenario: User updates their display name from account settings
    Given the user "alex@test.com" is logged in with password "Capstone26!"
    When the user updates their display name to "Alex Explorer"
    Then the display name "Alex Explorer" is shown on the dashboard
    And a success confirmation is displayed

  Scenario: Display name field is pre-populated in account settings
    Given the user "alex@test.com" is logged in with password "Capstone26!"
    When the user views the dashboard account settings
    Then the display name input is pre-populated with "Alex"

  Scenario: Validation rejects a display name that is too short
    Given the user "alex@test.com" is logged in with password "Capstone26!"
    When the user submits a display name of "X" from account settings
    Then the display name update is rejected with a validation error
