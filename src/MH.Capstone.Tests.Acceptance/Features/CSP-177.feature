@offline-queue
Feature: CSP-177 Offline Sightings Queue

    Scenario: Queue captures sighting while offline
        Given user Alex is logged in
        When Alex navigates to the sighting upload page
        And the device is simulated as offline
        And Alex fills in the sighting form
        And Alex submits the sighting form
        Then Alex is redirected to the offline queue page
        And the offline queue page shows at least one queued item

    Scenario: User inspects and manages queued items
        Given user Alex is logged in
        And Alex has a queued offline sighting
        When Alex navigates to the offline queue page
        Then the offline queue page shows at least one queued item
        And the queued item shows a delete button

    Scenario: Offline data is private to the device user
        Given user Alex is logged in
        And Alex has a queued offline sighting
        When Alex logs out
        And user Patricia logs in
        And Patricia navigates to the offline queue page
        Then Patricia sees no queued items belonging to Alex
