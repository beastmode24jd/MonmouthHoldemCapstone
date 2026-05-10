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

Scenario: Searching with an unrecognised term shows a polite no-results message
  Given user Alex is logged in
  And user is on the species search page
  When user searches for "xyznotananimal9999"
  Then user sees a polite no-results message

Scenario: The result counter updates to reflect a successful search
  Given user Alex is logged in
  And user is on the species search page
  When user searches for "eagle"
  Then the result counter shows at least one result

Scenario: The clear button resets the search state
  Given user Alex is logged in
  And user is on the species search page
  And user has searched for "eagle"
  When user clicks the clear button
  Then the search input is empty
  And the result card shows no results