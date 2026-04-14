Feature: Account Password Reset
    As a user of the Wildlife AID app
    I want to be able to reset my password
    So that I can still log into my account, even if I forget my password

@login
Scenario: Alex is looking for the Forgot Password page on the Login page.
    Given I am on the Login Page
    When I look at the Login input form
    Then I should see a Forgot Password link
    And it should change colors and my mouse cursor when I hover over it

@forgotPassword
Scenario: James has entered an account that does not exist in the database.
    Given I am on the Forgot Password page
    When I submit an account search for an account that does not exist
    Then I should see an error message saying the account was not found

@forgotPassword
Scenario: Alex has entered correct account parameters, and is being shown the “Confirm New Password” part of the form.
    Given I am on the Forgot Password page
    When I search for a valid account that exists
    Then I should be shown the two password fields

#Scenario: Alex is on the “Confirm New Password” page, but he has not written the same password twice for the password change.
    #Given I am on the Confirm New Password page
    #When I submit text in the new password boxes that do not match
    #And I click Save
    #Then I should see an error message telling me the inputs do not match


#Scenario: Alex is on the “Confirm New Password” page, and has submitted the same password twice.
    #Given I am on the Confirm New Password page
    #When I submit two matching text inputs in the new password boxes
    #And I click Save
    #Then I should see a confirmation message telling me my password was changed
    #And be redirected to the Login page