// CSP-211: Follower/Following tabs + NewFollower notification delivery.
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Seeding;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "csp211")]
[ExcludeFromCodeCoverage]
public class CSP211StepDefinitions
{
    private readonly AccountTabsDriver _tabs;
    private readonly AuthenticationDriver _auth;
    private readonly NotificationsDriver _notifications;
    private readonly IWebDriver _webDriver;

    public CSP211StepDefinitions(
        AccountTabsDriver tabs,
        AuthenticationDriver auth,
        NotificationsDriver notifications,
        IWebDriver webDriver)
    {
        _tabs = tabs;
        _auth = auth;
        _notifications = notifications;
        _webDriver = webDriver;
    }

    // -- Navigation -----------------------------------------------------------

    [When("Alex navigates to his own account page")]
    public void WhenAlexNavigatesToOwnAccount() => _tabs.NavigateToOwnAccount();

    [When("Alex navigates to Patricia's account page")]
    public void WhenAlexNavigatesToPatriciasAccount()
        => _tabs.NavigateToAccount(AcceptanceTestSeeder.PatriciaUserId);

    // -- Counts ---------------------------------------------------------------

    [Then("the follower count chip should read {int}")]
    public void ThenFollowerCountChipReads(int expected)
        => _tabs.GetFollowerCount().Should().Be(expected);

    [Then("the following count chip should read {int}")]
    public void ThenFollowingCountChipReads(int expected)
        => _tabs.GetFollowingCount().Should().Be(expected);

    // -- Tab clicks + list contents ------------------------------------------

    [When("Alex clicks the follower count")]
    public void WhenAlexClicksFollowerCount() => _tabs.ClickFollowerCount();

    [When("Alex clicks the following count")]
    public void WhenAlexClicksFollowingCount() => _tabs.ClickFollowingCount();

    [Then("the follower list should include {string}")]
    public void ThenFollowerListShouldInclude(string displayName)
        => _tabs.GetFollowerDisplayNames().Should().Contain(displayName);

    [Then("the following list should include {string}")]
    public void ThenFollowingListShouldInclude(string displayName)
        => _tabs.GetFollowingDisplayNames().Should().Contain(displayName);

    [Then("the row for {string} should link to her profile")]
    public void ThenRowLinksToProfile(string displayName)
    {
        var expectedId = ResolveUserId(displayName);
        _tabs.DoesFollowerRowLinkToProfile(displayName, expectedId)
            .Should().BeTrue($"the row for {displayName} should anchor to /account/{expectedId}");
        _tabs.DoesFollowerRowShowAvatar(displayName)
            .Should().BeTrue($"the row for {displayName} should render an avatar image");
    }

    private static Guid ResolveUserId(string displayName) => displayName switch
    {
        "Alex"     => AcceptanceTestSeeder.AlexUserId,
        "Lily"     => AcceptanceTestSeeder.LilyUserId,
        "Patricia" => AcceptanceTestSeeder.PatriciaUserId,
        _ => Guid.Empty,
    };

    // -- Empty states --------------------------------------------------------

    [Then("the followers tab should show the empty-state message")]
    public void ThenFollowersTabShowsEmptyState()
        => _tabs.IsFollowersEmptyStateVisible().Should().BeTrue();

    [Then("the following tab should show the empty-state message")]
    public void ThenFollowingTabShowsEmptyState()
        => _tabs.IsFollowingEmptyStateVisible().Should().BeTrue();

    // -- Follow + NewFollower notification delivery --------------------------

    [Given("Patricia has no NewFollower notifications yet")]
    public void GivenPatriciaHasNoNewFollowerNotifications()
    {
        // Seeder gives Patricia exactly one "Welcome to Wildlife AID!" notification.
        // This step is a documentary precondition; the assertion at the end of the
        // scenario verifies the *new* notification appears specifically.
    }

    [When("Alex follows Patricia")]
    public void WhenAlexFollowsPatricia()
    {
        // Use the existing UI flow: view Patricia's profile, click the Follow button.
        _tabs.NavigateToAccount(AcceptanceTestSeeder.PatriciaUserId);
        _webDriver.FindElement(By.Id("followButton")).Click();
        new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10))
            .Until(d => d.FindElements(By.Id("unfollowButton")).Count > 0);
    }

    [When("Patricia signs in to check her notifications")]
    public void WhenPatriciaSignsInToCheckNotifications()
    {
        _auth.LogoutUser();
        _auth.PreformLoginForUser("patricia@test.com", AcceptanceTestSeeder.TestPassword);
        _notifications.NavigateToNotifications();
    }

    [Then("Patricia should see a notification mentioning {string}")]
    public void ThenPatriciaShouldSeeNotificationMentioning(string keyword)
    {
        var rows = _webDriver.FindElements(By.CssSelector(".notification-row"));
        var anyMatch = rows.Any(r => r.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        anyMatch.Should().BeTrue(
            $"after Alex follows Patricia, at least one notification row should mention '{keyword}'");
    }
}
