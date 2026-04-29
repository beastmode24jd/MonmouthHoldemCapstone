@ai-recognition
Feature: AI Photo Recognition on Sighting Upload (CSP-144)
  As a Wildlife AID contributor uploading a sighting
  I want the app to suggest the species in my photo using AI
  So that I get an instant identification and a draft description
  without having to type one myself

  @functionality
  Scenario: Alex gets a species ID from a wildlife photo
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex selects a "wildlife" photo and clicks "Identify with AI"
    Then Alex should see a species suggestion badge with non-empty text
    And the Description field is populated with non-empty text

  @functionality
  Scenario: AI cannot identify a non-wildlife photo
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex selects a "non-wildlife" photo and clicks "Identify with AI"
    Then Alex should see a "could not identify" message
    And the Description field is not auto-filled

  @functionality
  Scenario: AI service failure leaves the form usable
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex selects a "service-down" photo and clicks "Identify with AI"
    Then Alex should see an AI error message
    And Alex can still type a description manually and submit the sighting
