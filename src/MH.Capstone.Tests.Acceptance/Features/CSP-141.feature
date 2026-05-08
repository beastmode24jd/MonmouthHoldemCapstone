@scoring
Feature: CSP-141 Species-Based Scoring

    Scenario: Mythic tier — first sighting of a new species earns maximum points
        Given user Alex is logged in
        When Alex submits a sighting of a brand new unique species
        Then the new species appears in Alex's Anidex with a "Mythic" rarity badge

    Scenario: Case-insensitive species name matching counts against the global total
        Given user Alex is logged in
        When Alex submits a sighting with species name "GREAT BLUE HERON"
        Then the species entry in Alex's Anidex shows a discovery count of 3
        And the species entry shows a "Mythic" rarity badge
