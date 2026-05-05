Feature: Badge Refinement
    As a Wildlife AID Clubs user
    I want to be able to check my Badges page
    So I can see Badges I have earned, and get hints on how to earn uncompleted Badges.

@justThisOne
Scenario: Alex is logged in and looking at their nav bar
    Given Alex is logged in
    When Alex looks at their nav bar
    Then Alex should see an option for a Badges page

@badge
Scenario: Alex has no badge progress on a badge
    Given I have no badge progress
    When I view my Badges page
    Then the Badge icon should be greyed out
    And give me a hint on how to start earning it

@badge
Scenario: Alex sees badge progression and next milestone
    Given I have partial progress on a multi-step badge
    When I view my Badge page
    Then a progress bar and the countdown remaining is displayed
    And a prompt is shown to guide my progress

@badge
Scenario: Alex's Badge page processes updates after relevant action
    Given I performs an action that advances Badge progress
    When the website processes my action
    Then the Badge page updates