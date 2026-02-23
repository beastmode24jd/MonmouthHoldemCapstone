/* Placeholder file text

Put Reqnroll/Gherkin tests from CSP-102 here, connect to user upload
of a text-block bio in their profile/account data model.

_____________________________________
User stories from Jira
[4 TOTAL!!!]
_____________________________________

Acceptance Criteria
Scenario: User is viewing their Dashboard page.

Given I am logged in on the Wildlife Finder App
And viewing my Dashboard page
When I am looking at my profile details
Then I should see a clear text label that says "Bio:", with text after it.

Scenario: User is uploading a text bio onto their account.

Given I am logged in on the Wildlife Finder App
And I am in the Settings card
And I have written a bio entry below or equal to 250 characters
When I click the "Submit Changes" button
Then I should see the "Bio:" field in my account details change
And see a clear confirmation message that my changes were saved successfully.

Scenario: User has not uploaded a custom text bio onto their account before.

Given I am logged in on the Wildlife Finder App
And I am in the Settings card
And I have not uploaded a bio entry below or equal to 250 characters
When I look at my account details in the upper Dashboard
Then I should see polite, placeholder text in the "Bio: " field, with a prompt to write a text bio for my account.

Scenario: User is uploading an empty bio to their account, after previously uploading a custom text bio.

Given I am logged in on the Wildlife Finder App
And I am in the Settings card
And I have previously saved a non-empty text bio field to my account display
When I upload an empty string text bio ("")
And I look at my account details in the upper Dashboard
Then I should see the "Bio: " field reset to the polite, placeholder text with a prompt to write a text bio for my account.

*/