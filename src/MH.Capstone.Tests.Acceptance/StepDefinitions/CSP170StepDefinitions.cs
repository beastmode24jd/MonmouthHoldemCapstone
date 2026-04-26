using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP170StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly SightingGalleryDriver _galleryDriver;

    public CSP170StepDefinitions(IWebDriver driver, SightingGalleryDriver galleryDriver)
    {
        _driver = driver;
        _galleryDriver = galleryDriver;
    }

    [Then("each sighting attribution shows a display name and not an email address")]
    public void ThenEachSightingAttributionShowsDisplayNameNotEmail()
    {
        var attributions = _galleryDriver.GetVisibleAttributionUsernames();

        attributions.Should().NotBeEmpty("the gallery should have at least one sighting with attribution");

        foreach (var name in attributions)
        {
            name.Should().NotContain("@",
                $"sighting attribution '{name}' must not be an email address");
        }
    }

    [Then("each leaderboard entry shows a display name and not an email address")]
    public void ThenEachLeaderboardEntryShowsDisplayNameNotEmail()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));

        rows.Count.Should().BeGreaterThan(0, "the leaderboard should contain at least one entry");

        foreach (var row in rows)
        {
            var cells = row.FindElements(By.TagName("td"));
            if (cells.Count < 2) continue;

            var displayName = cells[1].Text.Trim();
            displayName.Should().NotContain("@",
                $"leaderboard entry '{displayName}' must not be an email address");
        }
    }
}
