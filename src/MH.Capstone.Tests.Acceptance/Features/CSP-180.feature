@real-time-leaderboard
Feature: CSP-180 Real-Time Leaderboard And Notifications

    Scenario: Leaderboard updates are pushed to connected clients in real time
        Given user Alex is logged in
        And user Patricia is viewing the leaderboard in a second browser session
        When Alex earns additional points on the server
        Then Patricia's leaderboard view reflects the new points within 5 seconds without a page reload

    Scenario: Users with live notifications disabled do not receive in-app toasts
        Given user Lily is logged in
        And user Lily has disabled live notifications in settings
        And user Lily is viewing the leaderboard
        When Alex earns additional points on the server
        Then Lily does not see a live notification toast on her leaderboard view

    Scenario: A reconnecting client receives the current leaderboard snapshot
        Given user Patricia is logged in
        And user Patricia is viewing the leaderboard
        When the real-time connection drops and reconnects
        Then the leaderboard view reflects the current scores from the server
