@leaderboard
Feature: Points Leaderboard Display (CSP-XXX)
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
    Given there are multiple users with different point totals
    When I view the leaderboard
    Then users should be displayed in descending order by points
    And the user with the most points should appear first

  @functionality
  Scenario: Leaderboard limits display to maximum of 30 entries
    Given there are more than 30 users in the system
    When I view the leaderboard
    Then I should see a maximum of 30 user entries
    And the top 30 highest-scoring users should be shown

  @functionality
  Scenario: Logged-in user's entry is highlighted on leaderboard
    Given I am logged in as a user with points
    When I view the leaderboard
    Then my user entry should be visually highlighted
    And my current point total should be visible
    And I should be able to locate my entry easily

  @functionality
  Scenario: Users with zero points appear on leaderboard
    Given there are users with zero points in the system
    When I view the leaderboard
    Then users with zero points should be included in the list
    And they should appear after all users with positive points