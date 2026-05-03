Feature: CSP-172 Sighting Details Page

Scenario: Patricia opens a sighting's detail page from the gallery
    Given user Patricia is logged in
    And the user navigates to the gallery page
    When the user clicks the first sighting card
    Then the sighting details page is displayed
    And the details page shows the full image, uploader display name, profile icon, full description, timestamp, location, species name, and a fun fact about the animal

Scenario: Alex sees a friendly fallback when the fun fact cannot be retrieved
    Given user Alex is logged in
    When the user opens the sighting details page for a sighting with an unresolved species
    Then the details page still shows the image, uploader display name, profile icon, description, timestamp, and location
    And a friendly fallback message is shown in place of the fun fact

Scenario: James cannot view sighting details while signed out
    Given visitor James is signed out
    When the user navigates directly to a sighting details URL
    Then the user is redirected to the Login page

Scenario: Lily sees a not-found page when the sighting does not exist
    Given user Lily is logged in
    When the user navigates to a sighting details URL that does not match any sighting
    Then the sighting not found page is displayed
    And a link back to the Sighting Gallery is visible
