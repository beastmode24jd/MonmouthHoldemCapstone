// CSP-176: Mobile responsive redesign — viewport-driven layout assertions.
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "csp176")]
[ExcludeFromCodeCoverage]
public class CSP176StepDefinitions
{
    private readonly IWebDriver _webDriver;
    private readonly SightingGalleryDriver _galleryDriver;
    private readonly LeaderboardDriver _leaderboardDriver;

    public CSP176StepDefinitions(
        IWebDriver webDriver,
        SightingGalleryDriver galleryDriver,
        LeaderboardDriver leaderboardDriver)
    {
        _webDriver = webDriver;
        _galleryDriver = galleryDriver;
        _leaderboardDriver = leaderboardDriver;
    }

    // Restore a generous default window so subsequent (non-csp176) scenarios on the
    // same shared browser are not stuck at 360px or 1280px from this test.
    [AfterScenario("csp176")]
    public void RestoreDesktopViewport()
    {
        try { _webDriver.Manage().Window.Size = new Size(1920, 1080); } catch { /* nothing to restore */ }
    }

    [Given("the browser viewport is (.*) by (.*)")]
    public void GivenBrowserViewportIs(int width, int height)
    {
        _webDriver.Manage().Window.Size = new Size(width, height);
    }

    [When("Alex navigates to the gallery page")]
    public void WhenAlexNavigatesToTheGalleryPage()
    {
        _galleryDriver.NavigateToGallery();
    }

    [When("Alex navigates to the leaderboard page")]
    public void WhenAlexNavigatesToTheLeaderboardPage()
    {
        _leaderboardDriver.NavigateToLeaderboard();
    }

    [Then("the page should not require horizontal scrolling")]
    public void ThenPageShouldNotRequireHorizontalScrolling()
    {
        // documentElement.scrollWidth > clientWidth means content overflows the viewport horizontally.
        var overflowPx = (long)((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "return document.documentElement.scrollWidth - document.documentElement.clientWidth;");
        // Allow a 1px tolerance for sub-pixel rounding on some renderers.
        overflowPx.Should().BeLessThanOrEqualTo(1,
            $"the page should fit the viewport without horizontal scrolling; overflow was {overflowPx}px");
    }

    [Then("the sighting cards should occupy the full row width")]
    public void ThenSightingCardsShouldOccupyFullRowWidth()
    {
        // On mobile (col-12), each card wrapper's width should equal the row's width.
        // Permit a small delta because of Bootstrap's gutter padding (gx/gy classes add per-column padding).
        var ratio = (double)((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "var row = document.querySelector('#sightingsGrid'); " +
            "var card = document.querySelector('.sighting-card-wrapper'); " +
            "if (!row || !card) return 0; " +
            "return card.getBoundingClientRect().width / row.getBoundingClientRect().width;");
        ratio.Should().BeGreaterThan(0.9,
            "on mobile each sighting card should fill its row (col-12); ratio was {0:0.00}", ratio);
    }

    [Then("the sighting cards should be arranged in multiple columns")]
    public void ThenSightingCardsShouldBeArrangedInMultipleColumns()
    {
        // On desktop (col-md-4 / col-lg-3), each card occupies less than half the row.
        var ratio = (double)((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "var row = document.querySelector('#sightingsGrid'); " +
            "var card = document.querySelector('.sighting-card-wrapper'); " +
            "if (!row || !card) return 1; " +
            "return card.getBoundingClientRect().width / row.getBoundingClientRect().width;");
        ratio.Should().BeLessThan(0.5,
            "on desktop sightings should grid into multiple columns; ratio was {0:0.00}", ratio);
    }

    [Then("the leaderboard table should be inside a responsive wrapper")]
    public void ThenLeaderboardTableShouldBeInsideResponsiveWrapper()
    {
        _leaderboardDriver.TableIsInsideResponsiveWrapper()
            .Should().BeTrue("the <table> should be wrapped in a `.table-responsive` div so long names don't force the page to scroll horizontally");
    }

    [Then("the leaderboard table columns should all be visible")]
    public void ThenLeaderboardTableColumnsShouldAllBeVisible()
    {
        // On a wide desktop viewport, the table should fit without internal horizontal scroll.
        var overflowPx = (long)((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "var t = document.querySelector('table.table'); " +
            "if (!t) return -1; " +
            "return t.scrollWidth - t.clientWidth;");
        overflowPx.Should().BeLessThanOrEqualTo(1,
            $"the leaderboard table should fit on a desktop viewport without internal scroll; overflow was {overflowPx}px");
    }
}
