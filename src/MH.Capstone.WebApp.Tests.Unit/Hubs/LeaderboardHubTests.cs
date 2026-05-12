using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Hubs;

[TestFixture]
[ExcludeFromCodeCoverage]
public class LeaderboardHubTests
{
    private Mock<ILeaderboardService> _mockLeaderboardService = null!;
    private Mock<IHubCallerClients> _mockClients = null!;
    private Mock<ISingleClientProxy> _mockCallerProxy = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLeaderboardService = new Mock<ILeaderboardService>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockCallerProxy = new Mock<ISingleClientProxy>();
        _mockClients.Setup(c => c.Caller).Returns(_mockCallerProxy.Object);
    }

    private LeaderboardHub CreateSut()
    {
        var hub = new LeaderboardHub(_mockLeaderboardService.Object)
        {
            Clients = _mockClients.Object
        };
        return hub;
    }

    [Test]
    public async Task OnConnectedAsync_PushesSnapshotToCallerWithRanksAssignedByPosition()
    {
        var users = new List<ApplicationUser>
        {
            new() { Id = "u1", DisplayName = "Alex",     Points = 150 },
            new() { Id = "u2", DisplayName = "Patricia", Points = 100 },
            new() { Id = "u3", DisplayName = "Lily",      Points = 80 }
        };
        _mockLeaderboardService
            .Setup(s => s.GetLeaderboardPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(users);

        await CreateSut().OnConnectedAsync();

        _mockCallerProxy.Verify(p =>
            p.SendCoreAsync(
                LeaderboardHub.LeaderboardSnapshotEvent,
                It.Is<object?[]>(args =>
                    args.Length == 1 &&
                    args[0] is IReadOnlyList<LeaderboardEntryUpdate> &&
                    ((IReadOnlyList<LeaderboardEntryUpdate>)args[0]!).Count == 3 &&
                    ((IReadOnlyList<LeaderboardEntryUpdate>)args[0]!)[0].UserId == "u1" &&
                    ((IReadOnlyList<LeaderboardEntryUpdate>)args[0]!)[0].Rank == 1 &&
                    ((IReadOnlyList<LeaderboardEntryUpdate>)args[0]!)[2].Rank == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_EmptyLeaderboard_PushesEmptySnapshot()
    {
        _mockLeaderboardService
            .Setup(s => s.GetLeaderboardPageAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<ApplicationUser>());

        await CreateSut().OnConnectedAsync();

        _mockCallerProxy.Verify(p =>
            p.SendCoreAsync(
                LeaderboardHub.LeaderboardSnapshotEvent,
                It.Is<object?[]>(args =>
                    args.Length == 1 &&
                    args[0] is IReadOnlyList<LeaderboardEntryUpdate> &&
                    ((IReadOnlyList<LeaderboardEntryUpdate>)args[0]!).Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
