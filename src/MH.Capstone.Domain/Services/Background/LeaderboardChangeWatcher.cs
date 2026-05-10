using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services.Background
{
    // CSP-180: Polls the leaderboard at a fixed interval, detects per-user point
    // changes, and pushes them via ILiveBroadcastService. Truly additive — does
    // not modify ScoringService or LeaderboardService.
    public class LeaderboardChangeWatcher : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LeaderboardChangeWatcher> _logger;

        // Tracks the most recently observed points-per-user across the leaderboard page.
        private Dictionary<string, int> _lastPointsByUserId = new();

        public LeaderboardChangeWatcher(
            IServiceScopeFactory scopeFactory,
            ILogger<LeaderboardChangeWatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LeaderboardChangeWatcher started (interval: {Interval})", PollInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LeaderboardChangeWatcher tick failed; will retry next interval");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("LeaderboardChangeWatcher stopping");
        }

        private async Task TickAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var leaderboard = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();
            var broadcast = scope.ServiceProvider.GetRequiredService<ILiveBroadcastService>();

            var current = (await leaderboard.GetLeaderboardPageAsync(1)).ToList();
            var changes = DetectChanges(current, _lastPointsByUserId).ToList();

            foreach (var change in changes)
            {
                if (ct.IsCancellationRequested) break;
                await broadcast.BroadcastLeaderboardUpdateAsync(change);
            }

            _lastPointsByUserId = current.ToDictionary(u => u.Id, u => u.Points);
        }

        // Pure diff logic: compare current ranked snapshot against previous points map.
        // Emits one update per user whose points changed or who is newly present.
        public static IEnumerable<LeaderboardEntryUpdate> DetectChanges(
            IReadOnlyList<ApplicationUser> currentRanked,
            IReadOnlyDictionary<string, int> previousPointsByUserId)
        {
            for (int i = 0; i < currentRanked.Count; i++)
            {
                var user = currentRanked[i];
                int rank = i + 1;

                bool isNew = !previousPointsByUserId.TryGetValue(user.Id, out var prevPoints);
                if (isNew || prevPoints != user.Points)
                {
                    yield return new LeaderboardEntryUpdate
                    {
                        UserId = user.Id,
                        DisplayName = user.DisplayName,
                        Points = user.Points,
                        Rank = rank
                    };
                }
            }
        }
    }
}
