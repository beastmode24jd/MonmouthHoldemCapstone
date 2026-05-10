Feature: Admin Report System
    As a Wildlife AID admin
    I want to be able to check my Badges page
    So I can see Badges I have earned, and get hints on how to earn uncompleted Badges.

@report
Scenario: Non-moderator cannot access moderation tools
    Given a regular authenticated user logs in
    When they attempt to access the moderation queue URL
    Then access is denied and no moderation controls are visible

@report
Scenario: Moderator filters and views queue
    Given a moderator is authenticated
    When they open the moderation queue and apply filters (page, date, reporter)
    Then the queue list is filtered accordingly and results are paginated

@report
Scenario: Moderator performs a bulk dismiss
    Given multiple reports are selected
    When the moderator clicks Dismiss and confirms
    Then selected reports are marked dismissed and an audit log is created for each

@report
Scenario: Moderator soft-bans a user and creates an appeal entry
    Given a moderator reviews a user's repeated policy violations
    When they apply a soft-ban action with reason
    Then the user is marked as soft-banned and an appeal record is created
    And the action is recorded in the moderation audit log
