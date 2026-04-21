Feature: Profile Icon Upload
    As a Wildlife AID user
    I want to create dedicated private and/or public Clubs
    So we can track our point progress
    Ask questions
    Talk with each other
    Etc.

@frontPage
Scenario: James (user without pre-existing valid account) is on the front page
    Given I am on the front page
    When I look at the nav bar
    Then I should not see a Club page link

#@clubs
#Scenario: Alex is on the Clubs page, and wants to create a new club
#Given I am on the Clubs page
    #When I select valid options
    #And I click the Create New Club button
    #Then I should be redirected to the Club chatroom
    #And see the new club on my Clubs page

#@clubs
#Scenario: Alex has a new club, and wants to add Lily to it.
    #Given I am on the Clubs page
    #When I select valid options
    #And I click the Create New Club button
    #Then I should be able to invite another user
    #And they should see the Club invite on their Clubs page

#@clubs
#Scenario: Lily has created a private club, and Alex is not added to it.
    #Given I am on the Clubs page
    #When I select private for the Club
    #And do not add other users
    #Then my Club should not be visible on Alex's Club page