Feature: User Profile Page
    As a User, when I look up other Users,
    I want to learn more about them from their account Profile Page.

@profile
Scenario: Alex is viewing Lily’s User Profile page.
    Given I am looking at Lilys profile page
    When I look at the page details
    Then I can see her current point count
    And her recent Clubs
    And her recent Sightings

@profile
Scenario: Alex is looking at their own page.
    Given I am looking at my own profile page
    When I read the information provided
    Then I should see it update if I change my bio