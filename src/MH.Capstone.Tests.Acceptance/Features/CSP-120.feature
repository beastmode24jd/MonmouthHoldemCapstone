@ai-companion
Feature: AI Companion for Sightings (CSP-120)
  As a logged-in WildlifeAID user
  I want a globally-accessible AI chat companion
  So that I can ask wildlife-education and observer-safety questions from any page

  @functionality
  Scenario: Alex sees the AI Companion button when logged in
    Given Alex is logged in and viewing any page on the site
    Then Alex should see an "Ask the AI Companion" button

  @functionality
  Scenario: Alex asks a wildlife question and receives a reply
    Given Alex is logged in and viewing any page on the site
    When Alex opens the AI Companion chat
    And Alex asks "What should I know about black bear safety?"
    Then Alex should see a reply from the AI Companion

  @functionality
  Scenario: James (unauthenticated) cannot access the AI Companion
    Given James is not logged in
    When James visits a page on the site
    Then James should not see the "Ask the AI Companion" button

  @functionality
  Scenario: AI Companion refuses to handle off-topic prompts
    Given Alex is logged in and viewing any page on the site
    When Alex opens the AI Companion chat
    And Alex asks "Help me write a Python script to scrape a website"
    Then Alex should see a reply redirecting the conversation back to wildlife topics
