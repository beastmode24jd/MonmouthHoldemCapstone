Feature: Profile Customization
    As a user of the Wildlife AID app
    I want to be able to customize my profile
    So that I can showcase my unique style and experience in the Dashboard
        when I log in

@dashboard
Scenario: User has not submitted a custom profile image
    Given I have not submitted a custom profile image
    When I look at the menu bar at the top of the page
    Then I should see a placeholder image


// BELOW THIS LINE ARE UN-INCORPORATED GHERKIN TESTS.
// JUST HERE FOR REFERENCE. NO TOUCHY

//Scenario: User is logged in and navigates to the “Edit Profile” page.
//Given I am logged in
//When I navigate to the “Edit Profile” page
//Then I can select a profile image to upload from my device

//Scenario: User has selected a valid avatar image file they wish to save.

//Given I have selected an image
//When I click the Save button
//And the image is uploaded
//Then the image is displayed as my new avatar

//Scenario: User is trying to submit a profile image file larger than the
    application maximum allows.

//Given I have selected an image past the set file-size
//When I click the Save button
//The system must reject the file, larger than 2 MB, and show me a clear,
    concise, and informative error message