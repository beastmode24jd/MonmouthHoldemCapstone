using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "real-time-leaderboard")]
[ExcludeFromCodeCoverage]
public class CSP180StepDefinitions
{
    private const int PointsBoost = 50;
    private const string LilyEmail = "lily@test.com";
    private const string AlexEmail = "alex@test.com";
    private const string PatriciaEmail = "patricia@test.com";
    private const string TestPassword = "Capstone26!";

    private readonly AuthenticationDriver _authDriver;
    private readonly LeaderboardLiveDriver _leaderboardLive;
    private readonly LiveNotificationSettingsDriver _liveSettings;

    private string? _alexPointsBeforeOnSecondClient;
    private string? _patriciaPointsBeforeOnPrimary;

    public CSP180StepDefinitions(
        AuthenticationDriver authDriver,
        LeaderboardLiveDriver leaderboardLive,
        LiveNotificationSettingsDriver liveSettings)
    {
        _authDriver = authDriver;
        _leaderboardLive = leaderboardLive;
        _liveSettings = liveSettings;
    }

    [AfterScenario("real-time-leaderboard")]
    public void Cleanup()
    {
        _leaderboardLive.Dispose();
    }

    // ----- Givens -----

    [Given("user Patricia is viewing the leaderboard in a second browser session")]
    public void GivenPatriciaViewsLeaderboardInSecondSession()
    {
        _leaderboardLive.OpenSecondClient();
        _leaderboardLive.LoginSecondClientAs(PatriciaEmail, TestPassword);
        _leaderboardLive.NavigateSecondClientToLeaderboard();
        _alexPointsBeforeOnSecondClient =
            _leaderboardLive.GetPointsForUserOnSecondClient(AcceptanceTestSeeder.AlexUserId.ToString());
    }

    [Given("user Lily has disabled live notifications in settings")]
    public void GivenLilyHasDisabledLiveNotifications()
    {
        _liveSettings.DisableLiveNotifications();
    }

    [Given("user Lily is viewing the leaderboard")]
    [Given("user Patricia is viewing the leaderboard")]
    public void GivenUserIsViewingLeaderboard()
    {
        _leaderboardLive.NavigateToLeaderboard();
        _patriciaPointsBeforeOnPrimary =
            _leaderboardLive.GetPointsForUserOnPrimary(AcceptanceTestSeeder.PatriciaUserId.ToString());
    }

    // ----- Whens -----

    [When("Alex earns additional points on the server")]
    public async Task WhenAlexEarnsAdditionalPointsOnServer()
    {
        if (TestWebAppHost.Services is null)
            throw new InvalidOperationException("TestWebAppHost is not running.");

        using var scope = TestWebAppHost.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var alexId = AcceptanceTestSeeder.AlexUserId.ToString();
        var alex = await dbContext.Users.FirstAsync(u => u.Id == alexId);
        alex.Points += PointsBoost;
        await dbContext.SaveChangesAsync();
    }

    [When("the real-time connection drops and reconnects")]
    public void WhenLiveConnectionDropsAndReconnects()
    {
        _leaderboardLive.TriggerReconnectOnPrimary();
    }

    // ----- Thens -----

    [Then("Patricia's leaderboard view reflects the new points within 5 seconds without a page reload")]
    public void ThenSecondClientShowsUpdatedAlexPoints()
    {
        var updated = _leaderboardLive.WaitForPointsChangeOnSecondClient(
            AcceptanceTestSeeder.AlexUserId.ToString(),
            _alexPointsBeforeOnSecondClient ?? string.Empty,
            TimeSpan.FromSeconds(5));

        updated.Should().BeTrue(
            "Alex's points cell on Patricia's second-session leaderboard should change within 5 seconds via the live channel");
    }

    [Then("Lily does not see a live notification toast on her leaderboard view")]
    public void ThenLilyDoesNotSeeLiveToast()
    {
        Thread.Sleep(2000); // give the broadcast time to arrive (and be suppressed)
        var toastVisible = _leaderboardLive.LiveNotificationToastVisibleOnPrimary();
        toastVisible.Should().BeFalse(
            "Lily disabled live notifications, so no in-app toast should render even after a scoring event");
    }

    [Then("the leaderboard view reflects the current scores from the server")]
    public async Task ThenLeaderboardReflectsCurrentScoresAfterReconnect()
    {
        if (TestWebAppHost.Services is null)
            throw new InvalidOperationException("TestWebAppHost is not running.");

        using var scope = TestWebAppHost.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var patriciaId = AcceptanceTestSeeder.PatriciaUserId.ToString();
        var patricia = await dbContext.Users.FirstAsync(u => u.Id == patriciaId);
        var serverPoints = patricia.Points.ToString();

        // After reconnect, the snapshot should align the DOM with server-side truth.
        // We give the snapshot a short window to land before asserting.
        Thread.Sleep(2000);
        var displayedPoints =
            _leaderboardLive.GetPointsForUserOnPrimary(patriciaId);

        displayedPoints.Should().Be(serverPoints,
            "after reconnect the leaderboard snapshot should match the authoritative server score");
    }
}
