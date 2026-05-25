@csp211
Feature: CSP-211 Follower / Following Tabs & Notifications
    As a user,
    I want to see how many people follow me and how many I follow, click those counts to see who,
    and receive a notification when someone new follows me,
    so that the follow system feels like a real social feature.

    Background:
        Given user Alex is logged in

    # Seeder wires Alex<->Lily reciprocal; Patricia stays outside the graph.

    Scenario: Profile shows follower and following counts
        When Alex navigates to his own account page
        Then the follower count chip should read 1
        And the following count chip should read 1

    Scenario: Clicking the follower count opens the follower list
        When Alex navigates to his own account page
        And Alex clicks the follower count
        Then the follower list should include "Lily"
        And the row for "Lily" should link to her profile

    Scenario: Clicking the following count opens the following list
        When Alex navigates to his own account page
        And Alex clicks the following count
        Then the following list should include "Lily"

    Scenario: User with no follows shows empty-state messages
        When Alex navigates to Patricia's account page
        Then the follower count chip should read 0
        And the following count chip should read 0
        And the followers tab should show the empty-state message
        And the following tab should show the empty-state message

    # Patricia stays outside the seeded follow graph so this scenario triggers a
    # fresh NewFollower notification rather than a no-op idempotent re-follow.
    Scenario: New follower triggers a NewFollower notification for the followee
        Given Patricia has no NewFollower notifications yet
        When Alex follows Patricia
        And Patricia signs in to check her notifications
        Then Patricia should see a notification mentioning "Alex"
