Feature: CSP-133

Password Reset via Email

Scenario: Password Reset Email Request
  Given an anonymous user is on the Forgot Password page
  When they submit a password reset request with email "alice@test.com"
  Then they see a confirmation message that does not reveal whether the email is registered

Scenario: Reset Link Validity
  Given a valid password reset link has been generated for "alice@test.com"
  When the user navigates to the reset link
  Then the user sees the password reset form

Scenario: Successful Password Reset
  Given a valid password reset link has been generated for "newuser@test.com"
  When the user navigates to the reset link
  And the user submits new password "CSP133Reset1!" confirmed as "CSP133Reset1!"
  Then the user is redirected to the login page with a password reset success message
  And the user can log in with email "newuser@test.com" and password "CSP133Reset1!"

Scenario: Invalid or Expired Reset Link
  Given the user navigates to a reset link with an invalid token for "alice@test.com"
  Then the user sees an invalid reset link error message
  And the user sees an option to request a new reset link

Scenario: Multiple Reset Requests Invalidate Previous Tokens
  Given a first password reset link has been generated for "bob@test.com"
  And a second password reset link has been generated for "bob@test.com"
  When the user navigates to the first reset link and submits password "CSP133BobReset1!"
  Then the user sees an invalid reset link error message
