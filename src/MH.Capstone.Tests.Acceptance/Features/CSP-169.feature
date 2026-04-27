Feature: Notification Preferences Settings
    As a User, when I visit the settings section of my dashboard, I want to configure
    how I receive notifications by type so that I only get notified in the ways that are useful to me,
    while the system always ensures critical account notifications reach me regardless of my preferences.

@csp169
Scenario: User views notification preferences page
    Given Alex is logged in
    When Alex navigates to the notification preferences page
    Then the notification preferences form is displayed
    And the System/Account Critical Notifications type is not visible

@csp169
Scenario: User sets a notification type to Silence and saves
    Given Alex is logged in
    When Alex navigates to the notification preferences page
    And Alex sets "Badge Awarded" to "Silence"
    And Alex saves the notification preferences
    Then a success message is shown on the notification preferences page
    And the "Badge Awarded" preference is saved as "Silence"

@csp169
Scenario: User sets a notification type to Email Only and saves
    Given Alex is logged in
    When Alex navigates to the notification preferences page
    And Alex sets "Badge Awarded" to "Email Only"
    And Alex saves the notification preferences
    Then a success message is shown on the notification preferences page
    And the "Badge Awarded" preference is saved as "Email Only"

@csp169
Scenario: User sets a notification type to In-App & Email and saves
    Given Alex is logged in
    When Alex navigates to the notification preferences page
    And Alex sets "New Sighting Activity" to "In-App & Email"
    And Alex saves the notification preferences
    Then a success message is shown on the notification preferences page
    And the "New Sighting Activity" preference is saved as "In-App & Email"
