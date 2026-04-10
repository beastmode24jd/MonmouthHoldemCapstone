@report
Feature: Manual User Report of Page (CSP-101)
  As a logged-in user
  I want to report inappropriate or problematic pages
  So that the moderation team can review and take action

  @functionality
  Scenario: Alex successfully submits a report
    Given Alex is logged in and viewing a sighting page
    When Alex clicks "Report this page"
    And Alex selects a reason and optionally enters a description
    And Alex submits the form
    Then the report should be saved to the database
    And Alex should receive an in-app notification confirming the report was received

  @functionality
  Scenario: Report is stored with correct metadata
    Given Alex submits a report on a sighting page
    When the report is saved
    Then it should contain Alex's UserId, the page URL, the selected reason, and a SubmittedAt timestamp

  @functionality
  Scenario: Lily cannot report the same page twice
    Given Lily has already submitted a report for a specific page
    When Lily attempts to submit another report for the same page
    Then the system should reject the duplicate
    And Lily should see a message saying she has already reported this content

  @functionality
  Scenario: James (unauthenticated) cannot access the report feature
    Given James is not logged in
    When James visits a page on the site
    Then James should not see the "Report this page" button

  @functionality
  Scenario: Report appears in Patricia's admin review queue
    Given Alex has submitted a report
    When Patricia checks the admin review queue
    Then Alex's report should appear with status "Unresolved"
