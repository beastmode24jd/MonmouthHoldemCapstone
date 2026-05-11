using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.SignalR;

namespace MH.Capstone.WebApp.Hubs
{
    // CSP-180: SignalR endpoint for real-time leaderboard updates.
    // On connect, pushes the current leaderboard snapshot to the new client so
    // that latecomers and reconnecting clients see authoritative server state.
    public class LeaderboardHub : Hub
    {
        public const string LeaderboardSnapshotEvent = "LeaderboardSnapshot";

        private readonly ILeaderboardService _leaderboardService;

        public LeaderboardHub(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        public override async Task OnConnectedAsync()
        {
            var users = (await _leaderboardService.GetLeaderboardPageAsync(1)).ToList();

            var snapshot = users.Select((u, i) => new LeaderboardEntryUpdate
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                Points = u.Points,
                Rank = i + 1
            }).ToList();

            await Clients.Caller.SendAsync(LeaderboardSnapshotEvent, snapshot);

            await base.OnConnectedAsync();
        }
    }
}
