using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP37StepDefinitions
{
    // Alex's first seeded sighting (Great Blue Heron at the WOU campus pond). Owned by Alex,
    // so Patricia is a non-owner for it. ResetSeedDataAsync runs before AND after each @csp37
    // scenario, so editing this row never leaks into other features (CSP-142/CSP-172 depend on it).
    private static readonly Guid AlexSeededSightingId =
        new("a1000000-0000-0000-0000-000000000001");

    private const string SeededSpecies = "Great Blue Heron";
    private const string SeededDescription =
        "Great blue heron standing motionless at the WOU campus pond.";

    private readonly IWebDriver _driver;
    private readonly EditSightingDriver _editDriver;
    private readonly SightingDetailsDriver _detailsDriver;

    public CSP37StepDefinitions(
        IWebDriver driver,
        EditSightingDriver editDriver,
        SightingDetailsDriver detailsDriver)
    {
        _driver = driver;
        _editDriver = editDriver;
        _detailsDriver = detailsDriver;
    }

    // Wipes + reseeds before and after every @csp37 scenario so each starts from the known
    // seed state and never pollutes the sightings other features assert on.
    [BeforeScenario("csp37")]
    public static async Task ResetBefore() => await TestWebAppHost.ResetSeedDataAsync();

    [AfterScenario("csp37")]
    public static async Task ResetAfter() => await TestWebAppHost.ResetSeedDataAsync();

    [Given("Alex owns a seeded sighting")]
    public void GivenAlexOwnsASeededSighting()
    {
        // The sighting is provided by AcceptanceTestSeeder; this step documents the precondition.
    }

    [When("Alex opens the edit page for that sighting")]
    public void WhenAlexOpensTheEditPage()
    {
        _editDriver.NavigateToEdit(AlexSeededSightingId);
        _editDriver.IsOnEditPage().Should().BeTrue("the owner should reach the edit form");
    }

    [When("Alex submits species {string} with description {string}")]
    public void WhenAlexSubmitsSpeciesWithDescription(string species, string description)
    {
        _editDriver.SetSpecies(species);
        _editDriver.SetDescription(description);
        _editDriver.SubmitEdit();
    }

    [When("Alex submits species {string} with an empty description")]
    public void WhenAlexSubmitsSpeciesWithEmptyDescription(string species)
    {
        _editDriver.SetSpecies(species);
        _editDriver.SetDescription(string.Empty);
        _editDriver.SubmitEdit();
    }

    [When("Alex submits an empty species with description {string}")]
    public void WhenAlexSubmitsEmptySpeciesWithDescription(string description)
    {
        _editDriver.SetSpecies(string.Empty);
        _editDriver.SetDescription(description);
        _editDriver.SubmitEdit();
    }

    [When("Alex views the details page for that sighting")]
    [When("Patricia views the details page for Alex's sighting")]
    public void WhenViewsTheDetailsPage()
    {
        _detailsDriver.NavigateToDetails(AlexSeededSightingId);
    }

    [When("Patricia navigates directly to the edit page for Alex's sighting")]
    [When("James navigates directly to the edit page for that sighting")]
    public void WhenNonOwnerNavigatesToEditPage()
    {
        _editDriver.NavigateToEdit(AlexSeededSightingId);
    }

    [Then("Alex is taken to the sighting details page")]
    public void ThenAlexIsTakenToTheDetailsPage()
    {
        _detailsDriver.IsOnDetailsPage().Should().BeTrue(
            "a successful edit should redirect to /Sighting/Details/{id}");
    }

    [Then("the details page shows species {string}")]
    public void ThenTheDetailsPageShowsSpecies(string species)
    {
        var page = new SightingDetailsPageObject(_driver);
        page.Species.Text.Trim().Should().Be(species);
    }

    [Then("the details page shows description {string}")]
    public void ThenTheDetailsPageShowsDescription(string description)
    {
        var page = new SightingDetailsPageObject(_driver);
        page.Description.Text.Trim().Should().Be(description);
    }

    [Then("the species field contains {string}")]
    public void ThenTheSpeciesFieldContains(string expected)
    {
        _editDriver.GetSpeciesValue().Should().Be(expected,
            "the edit form should pre-fill the current species name");
    }

    [Then("the description field contains {string}")]
    public void ThenTheDescriptionFieldContains(string expected)
    {
        _editDriver.GetDescriptionValue().Should().Be(expected,
            "the edit form should pre-fill the current description");
    }

    [Then("an edit link is visible on the details page")]
    public void ThenAnEditLinkIsVisible()
    {
        var page = new SightingDetailsPageObject(_driver);
        page.EditLinks.Should().NotBeEmpty("the owner should see an edit link");
        page.EditLinks.First().Displayed.Should().BeTrue();
    }

    [Then("no edit link is visible on the details page")]
    public void ThenNoEditLinkIsVisible()
    {
        var page = new SightingDetailsPageObject(_driver);
        page.EditLinks.Should().BeEmpty("a non-owner must not see the edit link");
    }

    [Then("access to the edit page is denied")]
    public void ThenAccessToTheEditPageIsDenied()
    {
        // Authenticated non-owner → /Account/AccessDenied (Forbid); unauthenticated → /Account/Login.
        _editDriver.WasAccessDenied().Should().BeTrue(
            "a non-owner or unauthenticated visitor must not reach the edit form");
    }

    [Then("a validation error is shown on the edit form")]
    public void ThenAValidationErrorIsShown()
    {
        _editDriver.HasVisibleValidationError().Should().BeTrue(
            "an empty required field should surface a validation message");
    }

    [Then("the stored sighting species name is still {string}")]
    public void ThenTheStoredSpeciesIsStill(string expected)
    {
        // Re-open the details page from scratch to confirm the rejected edit never persisted.
        _detailsDriver.NavigateToDetails(AlexSeededSightingId);
        var page = new SightingDetailsPageObject(_driver);
        page.Species.Text.Trim().Should().Be(expected,
            "a rejected edit must not change the stored sighting");
    }
}
