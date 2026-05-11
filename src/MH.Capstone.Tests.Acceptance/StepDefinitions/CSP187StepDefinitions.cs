using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Seeding;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

// CSP-187: BDD steps for follow + comment + block + moderation.
[Binding]
[Scope(Tag = "csp187")]
[ExcludeFromCodeCoverage]
public class CSP187StepDefinitions
{
    // Lily's seeded "Wolverine" sighting (Crater Lake) — see AcceptanceTestSeeder.
    private static readonly Guid LilyWolverineSightingId =
        new("a2000000-0000-0000-0000-000000000001");

    private readonly IWebDriver _webDriver;
    private readonly AuthenticationDriver _authDriver;
    private readonly FeedDriver _feedDriver;
    private readonly ProfileDriver _profileDriver;
    private readonly CommentsDriver _commentsDriver;

    public CSP187StepDefinitions(
        IWebDriver webDriver,
        AuthenticationDriver authDriver,
        FeedDriver feedDriver,
        ProfileDriver profileDriver,
        CommentsDriver commentsDriver)
    {
        _webDriver = webDriver;
        _authDriver = authDriver;
        _feedDriver = feedDriver;
        _profileDriver = profileDriver;
        _commentsDriver = commentsDriver;
    }

    // Wipe + re-seed the DB before each csp-187 scenario so follow/block/comment state
    // from a prior scenario doesn't leak into this one. Also drop session cookies so the
    // "Given user X is logged in" steps actually re-authenticate.
    [BeforeScenario("csp187")]
    public async Task BeforeCsp187Scenario()
    {
        await TestWebAppHost.ResetSeedDataAsync();
        _webDriver.Manage().Cookies.DeleteAllCookies();
    }

    // -- Login ---------------------------------------------------------------

    // The "Given user Alex is logged in" / "Given user Patricia is logged in" steps
    // are already defined globally in other step files. We provide a csp-187-scoped
    // alias for Patricia's mid-scenario sign-in so the second login in scenario 4
    // doesn't collide with bindings that early-return on existing sessions.
    [Given("user Patricia is logged in for csp-187")]
    public void GivenPatriciaLoggedInForCsp187()
    {
        _authDriver.LogoutUser();
        _webDriver.Manage().Cookies.DeleteAllCookies();
        _authDriver.PreformLoginForUser("patricia@test.com", "Capstone26!");
    }

    // -- Follow / Unfollow ---------------------------------------------------

    [Given("Alex follows Lily")]
    [When("Alex follows Lily")]
    public void AlexFollowsLily()
    {
        _profileDriver.NavigateToProfile(AcceptanceTestSeeder.LilyUserId);
        if (_profileDriver.IsFollowButtonVisible())
            _profileDriver.ClickFollow();
    }

    // -- Feed ----------------------------------------------------------------

    [When("Alex navigates to the feed page")]
    public void WhenAlexNavigatesToFeed()
    {
        _feedDriver.NavigateToFeed();
    }

    [Then("the feed should contain at least one sighting by Lily")]
    public void ThenFeedContainsAtLeastOneLilySighting()
    {
        var lilyKey = AcceptanceTestSeeder.LilyUserId.ToString();
        var authorIds = _feedDriver.GetVisibleAuthorIds();
        authorIds.Should().Contain(lilyKey,
            "the feed should surface sightings by users Alex follows");
    }

    [Then("the feed should contain no sightings by Lily")]
    public void ThenFeedContainsNoLilySightings()
    {
        var lilyKey = AcceptanceTestSeeder.LilyUserId.ToString();
        var authorIds = _feedDriver.GetVisibleAuthorIds();
        authorIds.Should().NotContain(lilyKey,
            "blocked authors must be filtered out of the feed even if Alex still follows them");
    }

    // -- Block / Unblock -----------------------------------------------------

    [When("Alex blocks Lily")]
    public void WhenAlexBlocksLily()
    {
        _profileDriver.NavigateToProfile(AcceptanceTestSeeder.LilyUserId);
        if (_profileDriver.IsBlockButtonVisible())
            _profileDriver.ClickBlock();
    }

    // -- Comments ------------------------------------------------------------

    [Given("Alex opens Lily's wolverine sighting")]
    [When("Alex opens Lily's wolverine sighting")]
    public void AlexOpensLilysWolverineSighting()
    {
        _commentsDriver.NavigateToSightingDetails(LilyWolverineSightingId);
    }

    [When("Patricia opens Lily's wolverine sighting")]
    public void WhenPatriciaOpensLilysWolverineSighting()
    {
        _commentsDriver.NavigateToSightingDetails(LilyWolverineSightingId);
    }

    [Given("Alex posts the comment {string}")]
    [When("Alex posts the comment {string}")]
    public void AlexPostsTheComment(string body)
    {
        _commentsDriver.PostComment(body);
    }

    [Then("the comment {string} should be visible")]
    public void ThenCommentShouldBeVisible(string expectedBody)
    {
        var bodies = _commentsDriver.GetVisibleCommentBodies();
        bodies.Should().Contain(expectedBody,
            "the new comment should render on the details page after submission");
    }

    [Then("the comment {string} should not be visible")]
    public void ThenCommentShouldNotBeVisible(string expectedBody)
    {
        var bodies = _commentsDriver.GetVisibleCommentBodies();
        bodies.Should().NotContain(expectedBody,
            "hidden comments must be filtered from the visible list");
    }

    // -- Moderation ----------------------------------------------------------

    [When("Patricia hides the first visible comment")]
    public void WhenPatriciaHidesFirstVisibleComment()
    {
        _commentsDriver.HideFirstVisibleComment();
    }
}
