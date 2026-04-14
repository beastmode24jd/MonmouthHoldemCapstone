@ignore
Feature: Sightings Map
    As a user of the Wildlife AID app
    I want to view wildlife sightings on an interactive map
    So that I can see where animals have been spotted in my area

@map
Scenario: User views the sightings map
    Given I am a logged in user
    When I navigate to the sightings map page
    Then I should see an interactive map

@map
Scenario: Map displays sightings within bounds
    Given I am a logged in user
    And the following sightings exist:
        | Latitude | Longitude | Description           |
        | 44.9429  | -123.0351 | Deer near Salem       |
        | 45.5152  | -122.6784 | Eagle in Portland     |
    When I request sightings for bounds 44.0 to 46.0 latitude and -124.0 to -122.0 longitude
    Then I should receive 2 sightings

@map
Scenario: Map filters sightings outside bounds
    Given I am a logged in user
    And the following sightings exist:
        | Latitude | Longitude | Description           |
        | 44.9429  | -123.0351 | Deer near Salem       |
        | 34.0522  | -118.2437 | Coyote in Los Angeles |
    When I request sightings for bounds 44.0 to 46.0 latitude and -124.0 to -122.0 longitude
    Then I should receive 1 sighting

