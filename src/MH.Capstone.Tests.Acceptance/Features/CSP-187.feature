@csp187
Feature: CSP-187 Follow, comment, block, and moderation
    As an authenticated wildlife AID user
    I want to follow other users, comment on sightings, block unwanted users, and (as an admin) hide comments
    So that I can curate the content I see and keep the community healthy

    Scenario: Alex follows Lily and sees Lily's sightings in the feed
        Given user Alex is logged in
        When Alex follows Lily
        And Alex navigates to the feed page
        Then the feed should contain at least one sighting by Lily

    Scenario: Alex comments on Lily's wolverine sighting
        Given user Alex is logged in
        When Alex opens Lily's wolverine sighting
        And Alex posts the comment "Incredible — wolverines are so rare here!"
        Then the comment "Incredible — wolverines are so rare here!" should be visible

    Scenario: Alex blocks Lily and Lily's sightings disappear from his feed
        Given user Alex is logged in
        And Alex follows Lily
        When Alex blocks Lily
        And Alex navigates to the feed page
        Then the feed should contain no sightings by Lily

    Scenario: Patricia hides a comment and it disappears from the visible list
        Given user Alex is logged in
        And Alex opens Lily's wolverine sighting
        And Alex posts the comment "Comment slated for moderation"
        And user Patricia is logged in for csp-187
        When Patricia opens Lily's wolverine sighting
        And Patricia hides the first visible comment
        Then the comment "Comment slated for moderation" should not be visible
