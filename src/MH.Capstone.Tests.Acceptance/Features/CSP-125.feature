Feature: Clubs Feed
    As a Wildlife AID Clubs user
    I want to be able to check my friends and Club members' recent Sightings,
    so I can comment on and congratulate them in the Club chatroom.

@justThisOne
Scenario: Alex is on his front Club page
Given I am on the Club front page
When I look under the Club name and description
Then I should see the newest Sightings from the other Club members
And the front page should update if I upload a new Sighting

@clubs
Scenario: Lily leaves the Club, and Alex is on the front Club page
Given I am on the Club front page
And Lily leaves the Club
When I look under the Club name and description
Then I should see it update to remove Lily Sightings from the feed

