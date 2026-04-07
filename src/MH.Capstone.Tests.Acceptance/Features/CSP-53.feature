@auth
Feature: CSP-53

Sightings Upload

Scenario: Cannot upload invalid image file
  Given I am on the sightings upload page
  And I have an invalid image file
  When I attempt to upload the image file
  Then I should see a error/failure message.

Scenario: Cannot upload without all fields validated
  Given I am on the sightings upload page
  And I have not completed all the required fields
  When I attempt to submit the sightings upload form
  Then I should see a error/failure message.

Scenario: A valid upload completes and confirms
  Given I am on the sightings upload page
  And I have entered all valid and required information
  When I attempt to submit the sightings upload form
  Then I should see a success message or page.

@no-auth
Scenario: Non-Logged-in user cannot access page
  Given I am on the sightings upload page
  And I am an anonymous user
  When I attempt to access the sightings upload page
  Then I am denied access to the page.
