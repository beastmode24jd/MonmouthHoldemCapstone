Feature: Admin Report System
    As a Wildlife AID admin
    I want to be able to check my Badges page
    So I can see Badges I have earned, and get hints on how to earn uncompleted Badges.

@report
Scenario: Regular user cannot access admin tools
    Given a regular authenticated user logs in
    When they attempt to access the moderation queue URL
    Then access is denied and no moderation controls are visible

@justThisOne
Scenario: Admin filters and views queue
    Given a moderator is authenticated
    When they open the moderation queue and apply filters
    Then the queue list is filtered and results are paged

@report
Scenario: Admin performs a bulk dismiss
    Given multiple reports are selected
    When the moderator clicks Dismiss and confirms
    Then selected reports are marked dismissed and an audit log is created for each

@report
Scenario: Admin soft-locks a user account
    Given a moderator searches user accounts
    When they toggle a soft-lock on the account
    Then the account is marked as soft-locked and is unable to log in
