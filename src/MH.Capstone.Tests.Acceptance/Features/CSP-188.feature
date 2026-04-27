Feature: Account Settings Dedicated Page
    As a user, when on the dashboard, I want account settings in a dedicated page
    so that I can manage my profile and preferences more intuitively.

Background:
    Given Alex is logged in for settings

@csp188
Scenario: User navigates to settings page from dashboard
    When Alex visits the dashboard
    And Alex clicks the Account Settings link
    Then Alex is on the account settings page

@csp188
Scenario: Settings page displays all account settings sections
    When Alex navigates to the account settings page
    Then the display name form is visible
    And the notification preferences link is visible

@csp188
Scenario: Dashboard no longer shows account settings forms
    When Alex visits the dashboard
    Then the account settings forms are not shown on the dashboard
    And the Account Settings link is visible on the dashboard
