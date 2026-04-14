Feature: CSP-58

Wildlife Entry Search

Scenario: Species search page displays its expected UI elements
  Given user Alex is logged in
  When user navigates to the species search page
  Then the search input field is visible
  And the search button is visible
  And the clear button is visible
