@csp138
Feature: CSP-138

Bulk Notification Actions — Mark All as Read and Delete All

Scenario: User marks all notifications as read
  Given Alex is logged in and views their notifications
  And Alex has unread notifications
  When Alex selects Mark All as Read
  Then all notifications are marked as read
  And the Mark All as Read control is not visible
  And the notification count badge is cleared
  And the acceptance database is restored to its original seed state

Scenario: Mark All as Read is suppressed when no unread notifications exist
  Given Patricia is logged in and views their notifications
  Then the Mark All as Read control is not visible

Scenario: User deletes all notifications
  Given Alex is logged in and views their notifications
  And Alex has notifications
  When Alex selects Delete All
  Then the notification list displays an empty state message
  And the Delete All control is not visible
  And the notification count badge is cleared
  And the acceptance database is restored to its original seed state

Scenario: Delete All is suppressed when no notifications exist
  Given Alex has no notifications remaining
  When Alex views their notifications
  Then the Delete All control is not visible
  And the acceptance database is restored to its original seed state

Scenario: Bulk operations are scoped to the authenticated user
  Given Alex is logged in and views their notifications
  When Alex selects Delete All
  Then only Alex's notifications are deleted
  And Lily's notifications remain unchanged
  And the acceptance database is restored to its original seed state
