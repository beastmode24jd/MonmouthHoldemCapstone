using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Hubs;
using MH.Capstone.WebApp.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Services;

[TestFixture]
[ExcludeFromCodeCoverage]
public class LiveBroadcastServiceTests
{
    private const string TestUserId = "user-1";

    private Mock<IHubContext<LeaderboardHub>> _mockHubContext = null!;
    private Mock<IHubClients> _mockClients = null!;
    private Mock<IClientProxy> _mockAllClientsProxy = null!;
    private Mock<IClientProxy> _mockUserClientsProxy = null!;
    private Mock<ILiveNotificationPreferenceService> _mockPreferences = null!;

    [SetUp]
    public void SetUp()
    {
        _mockClients = new Mock<IHubClients>();
        _mockAllClientsProxy = new Mock<IClientProxy>();
        _mockUserClientsProxy = new Mock<IClientProxy>();
        _mockHubContext = new Mock<IHubContext<LeaderboardHub>>();
        _mockPreferences = new Mock<ILiveNotificationPreferenceService>();

        _mockHubContext.Setup(c => c.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(c => c.All).Returns(_mockAllClientsProxy.Object);
        _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockUserClientsProxy.Object);
    }

    private LiveBroadcastService CreateSut() =>
        new(_mockHubContext.Object, _mockPreferences.Object);

    #region BroadcastLeaderboardUpdateAsync

    [Test]
    public async Task BroadcastLeaderboardUpdateAsync_SendsLeaderboardUpdatedEventToAllClients()
    {
        var update = new LeaderboardEntryUpdate
        {
            UserId = TestUserId,
            DisplayName = "Alex",
            Points = 150,
            Rank = 3
        };

        await CreateSut().BroadcastLeaderboardUpdateAsync(update);

        _mockAllClientsProxy.Verify(p =>
            p.SendCoreAsync(
                LiveBroadcastService.LeaderboardUpdatedEvent,
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], update)),
                default),
            Times.Once);
    }

    [Test]
    public void BroadcastLeaderboardUpdateAsync_NullUpdate_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await CreateSut().BroadcastLeaderboardUpdateAsync(null!));
    }

    #endregion

    #region BroadcastNotificationToUserAsync

    [Test]
    public async Task BroadcastNotificationToUserAsync_UserOptedIn_SendsToUser()
    {
        _mockPreferences.Setup(p => p.IsEnabledAsync(TestUserId)).ReturnsAsync(true);
        var notification = new LiveNotification { Title = "T", Message = "M" };

        await CreateSut().BroadcastNotificationToUserAsync(TestUserId, notification);

        _mockClients.Verify(c => c.User(TestUserId), Times.Once);
        _mockUserClientsProxy.Verify(p =>
            p.SendCoreAsync(
                LiveBroadcastService.LiveNotificationEvent,
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], notification)),
                default),
            Times.Once);
    }

    [Test]
    public async Task BroadcastNotificationToUserAsync_UserOptedOut_DoesNotSend()
    {
        _mockPreferences.Setup(p => p.IsEnabledAsync(TestUserId)).ReturnsAsync(false);

        await CreateSut().BroadcastNotificationToUserAsync(
            TestUserId, new LiveNotification { Title = "T", Message = "M" });

        _mockClients.Verify(c => c.User(It.IsAny<string>()), Times.Never);
        _mockUserClientsProxy.Verify(p =>
            p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default),
            Times.Never);
    }

    [Test]
    public void BroadcastNotificationToUserAsync_NullNotification_Throws()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await CreateSut().BroadcastNotificationToUserAsync(TestUserId, null!));
    }

    [Test]
    public void BroadcastNotificationToUserAsync_NullOrWhitespaceUserId_Throws()
    {
        var sut = CreateSut();
        var notification = new LiveNotification { Title = "T", Message = "M" };

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.BroadcastNotificationToUserAsync(null!, notification));
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.BroadcastNotificationToUserAsync("   ", notification));
    }

    #endregion
}
