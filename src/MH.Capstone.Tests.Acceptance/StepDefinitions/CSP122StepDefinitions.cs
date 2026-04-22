using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions
{
    [Binding]
    [Scope(Tag = "photo-quality")]
    public class CSP122StepDefinitions
    {
        // Drivers (Selenium / DB helpers) will be injected here in Phase 2 once we know which ones we need. 

        #region Given

        [Given("Alex is on the Sighting Upload page")]
        public void GivenAlexIsOnTheSightingUploadPage()
        {
            throw new NotImplementedException("CSP-122: navigate Alex to the Sighting Upload page");
        }

        #endregion

        #region When

        [When("Alex submits a sighting with a {string} image")]
        [When("Alex submits a sighting with an {string} image")]
        public void WhenAlexSubmitsASightingWithAnImage(string imageQuality)
        {
            // imageQuality will be one of:
            //   "blurry", "low-light", "overexposed", "high-quality"
            throw new NotImplementedException(
                $"CSP-122: upload a '{imageQuality}' test image and submit the sighting form");
        }

        #endregion

        #region Then

        [Then("Alex should see the warning {string}")]
        public void ThenAlexShouldSeeTheWarning(string warningText)
        {
            throw new NotImplementedException(
                $"CSP-122: assert UI warning message is shown: '{warningText}'");
        }

        [Then("Alex should see the badge {string}")]
        public void ThenAlexShouldSeeTheBadge(string badgeText)
        {
            throw new NotImplementedException(
                $"CSP-122: assert UI success badge is shown: '{badgeText}'");
        }

        [Then("the saved sighting should have QualityTier {string}")]
        public void ThenTheSavedSightingShouldHaveQualityTier(string expectedTier)
        {
            throw new NotImplementedException(
                $"CSP-122: query DB for the new sighting and assert QualityTier == {expectedTier}");
        }

        [Then("the saved sighting's SharpnessScore should be recorded")]
        public void ThenTheSavedSightingsSharpnessScoreShouldBeRecorded()
        {
            throw new NotImplementedException(
                "CSP-122: query DB for the new sighting and assert SharpnessScore is not null");
        }

        [Then("the saved sighting's LuminanceAverage should be recorded")]
        public void ThenTheSavedSightingsLuminanceAverageShouldBeRecorded()
        {
            throw new NotImplementedException(
                "CSP-122: query DB for the new sighting and assert LuminanceAverage is not null");
        }

        #endregion
    }
}
