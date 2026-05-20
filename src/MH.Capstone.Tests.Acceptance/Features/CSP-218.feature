Feature: Club Chatroom Real-Time Messaging
    As a club member
    I want to send and receive messages in a club chatroom in real time
    So that club members can communicate without having to reload the page

@chatroom
Scenario: Alex navigates to her club's chatroom and sees the empty state
    Given I am on the Clubs page
    When I select valid options
    And I navigate to the chatroom for my new club
    Then I should see the empty chatroom placeholder

@chatroom
Scenario: Alex sends a message and it appears in the chatroom without a page reload
    Given I am on the Clubs page
    When I select valid options
    And I navigate to the chatroom for my new club
    And I send the message "Hello from the chatroom!"
    Then the message "Hello from the chatroom!" should appear in the chatroom

@chatroom
Scenario: A non-member cannot access a private club's chatroom
    Given I am on the Clubs page
    When I select private for the Club
    And I note the chatroom URL for my new club
    And I log in as Lily
    And I navigate directly to the noted chatroom URL
    Then I should be denied access to the chatroom
