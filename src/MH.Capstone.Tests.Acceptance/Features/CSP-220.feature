@csp220
Feature: CSP-220 Offline Queue Bug Fixes

    Scenario: Key offline pages are pre-cached after login
        Given user Alex is logged in
        When the service worker becomes active
        Then the home page is cached for offline use
        And the sighting create page is cached for offline use
        And the offline queue page is cached for offline use

    Scenario: Submitting a sighting while offline saves it to the queue
        Given user Alex is logged in
        When Alex navigates to the sighting upload page
        And the device is simulated as offline
        And Alex fills in the sighting form
        And Alex submits the sighting form
        Then Alex is redirected to the offline queue page
        And the offline queue shows at least one item with status pending

    @csp220-sync
    Scenario: Queued sighting syncs to the server when connectivity is restored
        Given user Alex is logged in
        And Alex has submitted a sighting while offline
        When Alex's device comes back online
        Then the queued sighting status changes to synced
        And the synced sighting appears in the sighting gallery
