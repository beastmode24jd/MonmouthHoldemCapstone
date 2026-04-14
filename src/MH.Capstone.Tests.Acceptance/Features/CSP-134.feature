Feature: CSP-134

Email Verification on Registration

Scenario: Email Sent on Registration
  Given a new user registers with a unique test email and password "Capstone26!"
  Then they are shown a registration confirmation page asking them to check their email

Scenario: Verification Link Works
  Given a new user registers with a unique test email and password "Capstone26!"
  And an email confirmation link has been generated for the registered email
  When the user navigates to the email confirmation link
  Then the user sees a successful email verification message
  And the user can log in with their registered email and password "Capstone26!"

Scenario: Invalid or Expired Verification Link
  Given a new user registers with a unique test email and password "Capstone26!"
  When the user navigates to a verification link with an invalid token
  Then the user sees a verification error message
  And the user sees an option to request a new verification link

Scenario: Restricted Access Before Verification
  Given a new user registers with a unique test email and password "Capstone26!"
  When the unverified user tries to log in with their registered email and password "Capstone26!"
  Then the user sees an email verification required message
  And the user sees an option to resend the verification email

Scenario: Resend Verification Email
  Given a new user registers with a unique test email and password "Capstone26!"
  When the user submits a resend verification request for their registered email
  Then they see a resend confirmation message
