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
