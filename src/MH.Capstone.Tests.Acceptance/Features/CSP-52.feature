Feature: CSP-52 Sightings Map
    As a user of the Wildlife AID app
    I want a dedicated map page I can view
    So that I can check if there are any nearby sightings

Background:
    Given I am using Chrome browser
    And the application is running

@map @selenium
Scenario: Map page requires authentication
    When I navigate to the map page without logging in
    Then I should be redirected to the login page

@map @selenium
Scenario: Map page displays for authenticated user
    Given I am logged in as a registered user
    When I navigate to the map page
    Then I should see the map container element

@map @selenium
Scenario: Map shows no sightings popup when area is empty
    Given I am logged in as a registered user
    When I navigate to the map page
    And there are no sightings in the current view
    Then I should see a popup indicating no sightings in the area

@map @selenium
Scenario: Map can be zoomed
    Given I am logged in as a registered user
    When I navigate to the map page
    Then I should be able to interact with the zoom controls