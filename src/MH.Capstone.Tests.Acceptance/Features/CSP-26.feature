Feature: CSP-26 Account Password Reset — UI Checks

@forgotPassword
Scenario: Forgot Password link is visible and hoverable on the Login page
    Given an anonymous user navigates to the login page
    When the user views the login form
    Then a "Forgot Password?" link is visible
    And the link changes appearance on hover

@forgotPassword
Scenario: Password mismatch shows a validation error on the reset form
    Given the user is on the reset password form for "alex@test.com"
    When the user enters new password "NewPass1!" and confirmation "DifferentPass1!"
    And the user submits the reset form
    Then a password confirmation mismatch error is visible
