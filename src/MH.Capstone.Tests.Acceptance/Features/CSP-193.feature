@csp193
Feature: CSP-193 Latitude & Longitude auto-populate via Geolocation API
    As an Authenticated User on the Sighting Upload page,
    I want my browser's geolocation to fill the Latitude and Longitude fields,
    so that I do not have to enter coordinates manually for the common case.

    Background:
        Given user Alex is logged in

    Scenario: Coordinates auto-populate when geolocation permission is granted
        Given the browser grants geolocation with latitude 44.04500 and longitude -123.07500
        When Alex navigates to the sighting upload page
        Then the latitude input should display "44.04500"
        And the longitude input should display "-123.07500"

    Scenario: User is informed when geolocation permission is denied
        Given the browser denies geolocation permission
        When Alex navigates to the sighting upload page
        Then the location status message should be visible
        And the location status message should mention entering coordinates manually
        And the latitude input should be empty or zero
        And the longitude input should be empty or zero

    Scenario: Form rejects submission when coordinates remain at the default of zero
        Given the browser denies geolocation permission
        When Alex navigates to the sighting upload page
        And Alex submits the sightings form
        Then a validation error should be shown about coordinates not being zero
