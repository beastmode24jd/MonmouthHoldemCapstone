using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions
{
    /// <summary>
    /// CSP-142: BDD steps for the personal Anidex page.
    /// Reuses the existing global "user Alex/Patricia is logged in" steps;
    /// these bindings cover only the Anidex-specific actions and assertions.
    /// </summary>
    [Binding]
    [Scope(Tag = "anidex")]
    public class CSP142StepDefinitions
    {
        private readonly IWebDriver _webDriver;
        private readonly AnidexDriver _anidexDriver;

        public CSP142StepDefinitions(IWebDriver webDriver, AnidexDriver anidexDriver)
        {
            _webDriver = webDriver;
            _anidexDriver = anidexDriver;
        }

        // Earlier scenarios in the suite can leave another user signed in
        // (AuthenticationDriver short-circuits if it spots an existing session
        // it can match by display name). Clearing cookies before each Anidex
        // scenario forces "Given user X is logged in" to authenticate fresh.
        [BeforeScenario("anidex")]
        public void ResetSessionCookies()
        {
            _webDriver.Manage().Cookies.DeleteAllCookies();
        }

        [When("Alex navigates to the Anidex page")]
        [When("Patricia navigates to the Anidex page")]
        public void WhenUserNavigatesToAnidex()
        {
            _anidexDriver.NavigateToAnidex();
            _anidexDriver.IsOnAnidexPage().Should().BeTrue("Anidex page should load successfully.");
        }

        [Then("Alex should see at least one species entry in the Anidex")]
        public void ThenAtLeastOneAnidexEntry()
        {
            _anidexDriver.IsEmptyStateVisible().Should().BeFalse("Alex has seeded sightings, so empty state must not show.");
            _anidexDriver.GetVisibleEntryCount().Should().BeGreaterThan(0);
        }

        [Then("every visible Anidex entry should show a species name and a rarity badge")]
        public void ThenEveryEntryHasNameAndRarityBadge()
        {
            _anidexDriver.EveryEntryHasNameAndRarityBadge().Should().BeTrue();
        }

        [Then("the Anidex empty state should be visible")]
        public void ThenAnidexEmptyStateVisible()
        {
            _anidexDriver.IsEmptyStateVisible().Should().BeTrue();
            _anidexDriver.GetVisibleEntryCount().Should().Be(0);
        }

        [Then("the {string} Anidex entry should display a discovery count of (.*)")]
        public void ThenSpeciesDiscoveryCountIs(string speciesName, int expectedCount)
        {
            var count = _anidexDriver.GetDiscoveryCountFor(speciesName);
            count.Should().NotBeNull(
                $"the user's Anidex should contain a '{speciesName}' entry");
            count!.Value.Should().Be(expectedCount);
        }

        [Then("Alex's Anidex should not contain a {string} entry")]
        public void ThenAnidexShouldNotContainSpecies(string speciesName)
        {
            var visibleSpecies = _anidexDriver.GetVisibleSpeciesNames();
            visibleSpecies.Should().NotContain(speciesName,
                $"only Alex's own confirmed species should appear in his Anidex");
        }
    }
}
