Feature: CSP-53

Sightings Upload

Scenario: Cannot upload invalid image file
  Given user Alpha is logged in
  And user is on the sightings upload page
  And user has an invalid image file
  When user attempts to upload the image file
  Then user should see a error/failure message.

Scenario: Cannot upload without all fields validated
  Given user Alpha is logged in 
  And user is on the sightings upload page
  And user has not completed all the required fields
  When user attempts to submit the sightings upload form
  Then user should see a error/failure message.

Scenario: A valid upload completes and confirms
  Given user Alpha is logged in 
  And user is on the sightings upload page
  And user has entered all valid and required information
  When user attempts to submit the sightings upload form
  Then user should be redirected to their dashboard.

Scenario: Non-Logged-in user cannot access page
  Given an unauthenticated user
  When user attempts to access the sightings upload page
  Then user is denied access to the page.
