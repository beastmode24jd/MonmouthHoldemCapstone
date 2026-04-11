using System;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions
{
    [Binding]
    public class CSP53StepDefinitions
    {
        private readonly SightingsDriver _sightingsDriver;
        private readonly AuthenticationDriver _authenticationDriver;
        private readonly DashboardDriver _dashboardDriver;

        public CSP53StepDefinitions(SightingsDriver sightingsDriver, 
            AuthenticationDriver authenticationDriver, DashboardDriver dashboardDriver)
        {
            _sightingsDriver = sightingsDriver;
            _authenticationDriver = authenticationDriver;
            _dashboardDriver = dashboardDriver;
        }

        [Given("user Alex is logged in")]
        public void GivenUserAlexIsLoggedIn()
        {
            _authenticationDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
        }

        [Given("user is on the sightings upload page")]
        public void GivenUserIsOnTheSightingsUploadPage()
        {
            _sightingsDriver.NavigateToSightingsUpload();
        }

        [Given("user has an invalid image file")]
        public void GivenUserHasAnInvalidImageFile()
        {
            throw new PendingStepException();
        }

        [When("user attempts to upload the image file")]
        public void WhenUserAttemptsToUploadTheImageFile()
        {
            throw new PendingStepException();
        }

        [Then("user should see a error\\/failure message.")]
        public void ThenUserShouldSeeAErrorFailureMessage()
        {
            throw new PendingStepException();
        }

        [Given("user has not completed all the required fields")]
        public void GivenUserHasNotCompletedAllTheRequiredFields()
        {
            throw new PendingStepException();
        }

        [When("user attempts to submit the sightings upload form")]
        public void WhenUserAttemptsToSubmitTheSightingsUploadForm()
        {
            throw new PendingStepException();
        }

        [Given("user has entered all valid and required information")]
        public void GivenUserHasEnteredAllValidAndRequiredInformation()
        {
            throw new PendingStepException();
        }

        [Then("user should be redirected to their dashboard.")]
        public void ThenUserShouldBeRedirectedToTheirDashboard()
        {
            _dashboardDriver.IsOnDashboard().Should().BeTrue();
        }

        [Given("an unauthenticated user")]
        public void GivenAnUnauthenticatedUser()
        {
            _authenticationDriver.PreformLoginForUser("james@test.com", "Capstone26!");
        }

        [When("user attempts to access the sightings upload page")]
        public void WhenUserAttemptsToAccessTheSightingsUploadPage()
        {
            _sightingsDriver.NavigateToSightingsUpload();
        }

        [Then("user is denied access to the page.")]
        public void ThenUserIsDeniedAccessToThePage()
        {
            _authenticationDriver.WasPageAccessDenied().Should().BeTrue();
        }

    }
}
