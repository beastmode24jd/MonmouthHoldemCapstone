Feature: Admin Action Audit Logs
    As an Admin, when I or another moderator performs an administrative action,
    I want every moderator action to be recorded in an audit log
    so that the team can review action history,
    investigate disputes,
    and ensure accountability.

@justThisOne
Scenario: Audit log page is not accessible to regular users
    Given Alex is logged in
    When Alex navigates directly to the audit log page URL
    Then Alex receives an access denied response

@audit
Scenario: Resolving a report creates an audit log entry
    Given an admin is logged in
    And an unresolved report exists
    When the admin resolves the report
    And the admin navigates to the audit log page
    Then an entry is visible for the Report Resolved action
    And the entry shows the admin's display name and a recent timestamp

@audit
Scenario: Locking a user creates an audit log entry
    Given an admin is logged in
    And an active user account exists
    When the admin locks that user account
    And the admin navigates to the audit log page
    Then an entry is visible for the locking action
    And the entry references the locked user

@audit
Scenario: Unlocking a user creates an audit log entry
    Given an admin is logged in
    And a locked user account exists
    When the admin unlocks that user account
    And the admin navigates to the audit log page
    Then an entry is visible for the unlocking action
    And the entry references the unlocked user