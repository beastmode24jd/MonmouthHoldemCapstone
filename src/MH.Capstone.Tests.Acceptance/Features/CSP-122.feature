@photo-quality
Feature: Photo Quality Gate at Sighting Upload (CSP-122)
  As a Wildlife AID contributor uploading a sighting
  I want the app to evaluate my photo for blur, exposure, and resolution at submission
  So that I get instructional feedback and my sighting record captures quality metadata

  @functionality
  Scenario: Alex uploads a blurry photo and sees a helpful warning
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with a "blurry" image
    Then Alex should see the warning "This photo looks a bit blurry - steady your camera or try again"
    And the saved sighting should have QualityTier "Low"
    And the saved sighting's SharpnessScore should be recorded

  @functionality
  Scenario: Alex uploads a photo taken in low light
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with a "low-light" image
    Then Alex should see the warning "This photo is too dark - try finding better light"
    And the saved sighting should have QualityTier "Low"
    And the saved sighting's LuminanceAverage should be recorded

  @functionality
  Scenario: Alex uploads an washed-out photo
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with an "overexposed" image
    Then Alex should see the warning "This photo is washed out - try adjusting exposure"
    And the saved sighting should have QualityTier "Low"

  @functionality
  Scenario: Alex uploads a sharp, well lit, high resolution photo
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with a "high-quality" image
    Then Alex should see the badge "Ready for ID - High Quality"
    And the saved sighting should have QualityTier "High"


