@photo-quality
Feature: Photo Quality Gate at Sighting Upload (CSP-122 / CSP-189)
  As a Wildlife AID contributor uploading a sighting
  I want the app to evaluate my photo for blur, exposure, and resolution at submission
  So that low-quality photos are rejected with a clear reason and good photos are confirmed

  @functionality
  Scenario: Alex uploads a blurry photo and the upload is rejected with a clear reason
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with a "blurry" image
    Then Alex should see the upload error mentioning "blurry"
    And the upload stays on the Sighting Upload page
    And no sighting was saved for that upload

  @functionality
  Scenario: Alex uploads a photo taken in low light and the upload is rejected
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with a "low-light" image
    Then Alex should see the upload error mentioning "dark"
    And the upload stays on the Sighting Upload page
    And no sighting was saved for that upload

  @functionality
  Scenario: Alex uploads an overexposed photo and the upload is rejected
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with an "overexposed" image
    Then Alex should see the upload error mentioning "overexposed"
    And the upload stays on the Sighting Upload page
    And no sighting was saved for that upload

  @functionality
  Scenario: Alex uploads a sharp, well lit, high resolution photo
    Given user Alex is logged in
    And Alex is on the Sighting Upload page
    When Alex submits a sighting with a "high-quality" image
    Then Alex should see the success message "Great photo! Upload accepted."
    And the saved sighting should have QualityTier "High"
