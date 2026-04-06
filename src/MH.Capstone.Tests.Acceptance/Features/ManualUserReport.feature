@report
Feature: Manual User Report of Page (CSP-101)
  As a logged-in user
  I want to report inappropriate or problematic pages
  So that the moderation team can review and take action

  @functionality
  Scenario: User successfully submits a report
    Given I am logged in and viewing a sighting page
    When I click "Report this page"
    And I select a reason and optionally enter a description
    And I submit the form
    Then the report should be saved to the database
    And I should receive an in-app notification confirming my report was received

  @functionality
  Scenario: Report is stored with correct metadata
    Given I submit a report on a sighting page
    When the report is saved
    Then it should contain my UserId, the page URL, the selected reason, and a SubmittedAt timestamp

  @functionality
  Scenario: User cannot report the same page twice
    Given I have already submitted a report for a specific page
    When I attempt to submit another report for the same page
    Then the system should reject the duplicate
    And I should see a message saying I have already reported this content

  @functionality
  Scenario: Unauthenticated user cannot submit a report
    Given I am not logged in
    When I attempt to access the report submission endpoint
    Then I should be redirected to the login page

  @functionality
  Scenario: Report appears in admin review queue
    Given a user has submitted a report
    When an admin views the reports panel
    Then the report should appear with status "Unresolved"
