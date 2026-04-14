Feature: CSP-47 Account Deactivation
    As a registered user
    I want to be able to deactivate my account
    So that I have full control over my personal data and digital presence

Background:
    Given I am using Chrome browser
    And the application is running

@deactivation @selenium
Scenario: Access deactivation page requires authentication
    When I navigate to the deactivate page without logging in
    Then I should be redirected to the login page

@deactivation @selenium
Scenario: Deactivation page displays warning message
    Given I am logged in as a registered user
    When I navigate to the deactivate page
    Then I should see a warning about account deactivation consequences
    And I should see a password confirmation field

@deactivation @selenium
Scenario: Failed deactivation with incorrect password
    Given I am logged in as a registered user
    When I navigate to the deactivate page
    And I enter an incorrect password "WrongPassword123!"
    And I click the deactivate button
    Then I should see an error message about incorrect password
    And I should remain on the deactivate page

@deactivation @selenium
Scenario: Successful account deactivation
    Given I have registered a new test account
    And I am logged in with the test account
    When I navigate to the deactivate page
    And I enter the correct password
    And I click the deactivate button
    Then I should be redirected to the login page
    And I should see a message that my account has been deactivated
