Feature: CSP-58

Wildlife Entry Search

Scenario: Species search page displays its expected UI elements
  Given user Alex is logged in
  When user navigates to the species search page
  Then the search input field is visible
  And the search button is visible
  And the clear button is visible

Scenario: Searching by a known animal name displays a result card
  Given user Alex is logged in
  And user is on the species search page
  When user searches for "eagle"
  Then a result card is displayed with an animal name
