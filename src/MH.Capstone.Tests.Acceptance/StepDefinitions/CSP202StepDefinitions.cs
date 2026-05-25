using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions
{
    /// <summary>
    /// CSP-202: Anidex card -> per-species detail dialog (photo + description + timestamp).
    /// Reuses the global "user X is logged in" binding; defines its own Anidex navigation
    /// step because CSP-142's navigation step is scoped to the @anidex tag, not @csp202.
    /// </summary>
    [Binding]
    [Scope(Tag = "csp202")]
    public class CSP202StepDefinitions
    {
        private readonly IWebDriver _webDriver;
        private readonly AnidexDriver _anidexDriver;

        public CSP202StepDefinitions(IWebDriver webDriver, AnidexDriver anidexDriver)
        {
            _webDriver = webDriver;
            _anidexDriver = anidexDriver;
        }

        // Other features (CSP-37 edits Alex's Great Blue Heron, CSP-211 mutates
        // follow state, etc.) can leave Alex's sightings in a non-seed state.
        // Reset before each csp202 scenario so the discovery counts we assert on
        // match the seeded baseline.
        [BeforeScenario("csp202")]
        public async Task ResetBeforeScenario()
        {
            await TestWebAppHost.ResetSeedDataAsync();
            _webDriver.Manage().Cookies.DeleteAllCookies();
        }

        [When("Alex navigates to the Anidex page")]
        public void WhenAlexNavigatesToTheAnidexPage()
        {
            _anidexDriver.NavigateToAnidex();
            _anidexDriver.IsOnAnidexPage().Should().BeTrue("Anidex page should load successfully.");
        }

        [When("Alex clicks the {string} species card")]
        public void WhenAlexClicksTheSpeciesCard(string speciesName)
        {
            _anidexDriver.ClickSpeciesCard(speciesName);
        }

        [When("Alex closes the sightings dialog")]
        public void WhenAlexClosesTheSightingsDialog()
        {
            _anidexDriver.CloseOpenDialog();
        }

        [Then("the {string} sightings dialog is shown")]
        public void ThenTheSightingsDialogIsShown(string speciesName)
        {
            _anidexDriver.IsDialogShownFor(speciesName).Should().BeTrue(
                $"clicking the '{speciesName}' card should open its per-sighting dialog");
        }

        [Then("the {string} sightings dialog is not shown")]
        public void ThenTheSightingsDialogIsNotShown(string speciesName)
        {
            _anidexDriver.IsDialogShownFor(speciesName).Should().BeFalse(
                $"closing the dialog should hide the '{speciesName}' per-sighting list");
        }

        [Then("the {string} sightings dialog lists {int} entries")]
        public void ThenTheSightingsDialogListsEntries(string speciesName, int expectedCount)
        {
            _anidexDriver.GetDialogEntryCountFor(speciesName).Should().Be(expectedCount,
                $"the dialog should list every per-user sighting under '{speciesName}'");
        }

        [Then("the {string} card does not open a sightings dialog")]
        public void ThenTheCardDoesNotOpenADialog(string speciesName)
        {
            _anidexDriver.CardOpensDialog(speciesName).Should().BeFalse(
                $"single-sighting species like '{speciesName}' should not be clickable into a dialog");
        }
    }
}
