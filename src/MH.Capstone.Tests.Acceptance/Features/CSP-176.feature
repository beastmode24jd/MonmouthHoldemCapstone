@csp176
Feature: CSP-176 Mobile responsive redesign
    As a mobile user (phone, ~360px viewport),
    I want gallery, leaderboard, and submission pages to adapt cleanly to small screens,
    so that I can browse and submit sightings without horizontal scrolling or unreachable controls.

    Background:
        Given user Alex is logged in

    @mobile
    Scenario: Gallery has no horizontal scroll on a 360px viewport
        Given the browser viewport is 360 by 800
        When Alex navigates to the gallery page
        Then the page should not require horizontal scrolling

    @mobile
    Scenario: Gallery cards stack into a single column on a 360px viewport
        Given the browser viewport is 360 by 800
        When Alex navigates to the gallery page
        Then the sighting cards should occupy the full row width

    @mobile
    Scenario: Leaderboard has no horizontal page scroll on a 360px viewport
        Given the browser viewport is 360 by 800
        When Alex navigates to the leaderboard page
        Then the page should not require horizontal scrolling

    @mobile
    Scenario: Leaderboard table is wrapped for safe overflow on mobile
        Given the browser viewport is 360 by 800
        When Alex navigates to the leaderboard page
        Then the leaderboard table should be inside a responsive wrapper

    @desktop
    Scenario: Gallery shows multi-column layout on desktop (no regression)
        Given the browser viewport is 1280 by 800
        When Alex navigates to the gallery page
        Then the page should not require horizontal scrolling
        And the sighting cards should be arranged in multiple columns

    @desktop
    Scenario: Leaderboard renders full table on desktop (no regression)
        Given the browser viewport is 1280 by 800
        When Alex navigates to the leaderboard page
        Then the page should not require horizontal scrolling
        And the leaderboard table columns should all be visible
