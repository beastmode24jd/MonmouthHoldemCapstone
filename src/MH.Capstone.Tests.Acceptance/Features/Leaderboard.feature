@leaderboard
Feature: Points Leaderboard Display (CSP-97)
  As a user or visitor of the Wildlife AID app
  I want to view the leaderboard with rankings by points
  So I can see how I compare to other players and view top contributors

  @elementcheck
  Scenario: Navigation bar contains Leaderboard link
    Given I am on the home page
    When I view the navigation bar
    Then I should see a "Leaderboard" link

  @functionality
  Scenario: Leaderboard displays users in descending order by points
    Given Patricia has 500 points
    And Alex has 250 points
    And Lily has 100 points
    When I view the leaderboard
    Then users should be displayed in descending order by points
    And Patricia should appear above Alex
    And Alex should appear above Lily

  @functionality
  Scenario: Leaderboard limits display to maximum of 30 entries
    Given there are more than 30 users in the system
    When I view the leaderboard
    Then I should see a maximum of 30 user entries
    And the top 30 highest-scoring users should be shown

  @functionality
  Scenario: Logged-in user's entry is highlighted on leaderboard
    Given Patricia has 500 points
    And Lily has 100 points
    And Alex is logged in with 300 points
    When Alex views the leaderboard
    Then Alex's entry should be visually highlighted
    And Alex's point total of 300 should be visible
    And Alex should be able to locate their entry easily

  @functionality
  Scenario: Users with zero points appear on leaderboard
    Given Patricia has 150 points
    And Alex has 0 points
    And Lily has 0 points
    When I view the leaderboard
    Then Alex and Lily should be included in the list with zero points
    And they should appear after all users with positive points
