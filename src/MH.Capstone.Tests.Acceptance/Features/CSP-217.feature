@csp217
Feature: CSP-217 Sighting-map timestamps in user's local timezone
    As an authenticated user,
    I want the sightings-map popup timestamps to render in my local time,
    so that "Spotted" times match my own clock instead of UTC.

    Background:
        Given user Alex is logged in
        And Alex opens the sightings map

    Scenario: Timestamps reflect the UserTimeZone cookie
        When Alex fetches sightings with UserTimeZone "Etc/UTC"
        And Alex fetches sightings with UserTimeZone "America/Los_Angeles"
        Then the two timestamp sets should differ

    Scenario: Map cannot be panned past the international date line
        When Alex pans the map to longitude 500
        Then the map's center longitude should be between -180 and 180
