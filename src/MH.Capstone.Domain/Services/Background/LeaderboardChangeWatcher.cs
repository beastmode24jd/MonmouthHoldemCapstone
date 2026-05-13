using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MH.Capstone.Domain.Services.Background
{
    // CSP-180: Polls the leaderboard at a fixed interval, detects per-user point
    // changes, and pushes them via ILiveBroadcastService. Truly additive — does
    // not modify ScoringService or LeaderboardService.
    public class LeaderboardChangeWatcher : BackgroundService
    {
        private readonly TimeSpan _pollInterval;

        // Rank-change toasts only fire for users involved in the top N positions.
        // A user moving from rank 9 → 11 still notifies (oldRank ≤ N), so users
        // who get bumped out of the top group still hear about it.
        private const int RankChangeTopN = 10;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LeaderboardChangeWatcher> _logger;

        // Tracks the most recently observed points-per-user across the leaderboard page.
        private Dictionary<string, int> _lastPointsByUserId = new();

        // Tracks the most recently observed rank-per-user so we can notify users
        // whose rank changed even if their own points didn't (i.e. they got passed).
        private Dictionary<string, int> _lastRankByUserId = new();

        public LeaderboardChangeWatcher(
            IServiceScopeFactory scopeFactory,
            ILogger<LeaderboardChangeWatcher> logger,
            IOptions<LeaderboardWatcherOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _pollInterval = TimeSpan.FromHours(options.Value.PollIntervalHours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LeaderboardChangeWatcher started (interval: {Interval})", _pollInterval);

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
                    await Task.Delay(_pollInterval, stoppingToken);
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

            // Per-user rank-change toasts. Only fires for users involved in the
            // top-N positions (so a leaderboard with thousands of users doesn't
            // spam notifications for every micro-rank shuffle further down).
            var rankChanges = DetectRankChanges(current, _lastRankByUserId, RankChangeTopN).ToList();
            foreach (var (userId, oldRank, newRank) in rankChanges)
            {
                if (ct.IsCancellationRequested) break;
                await broadcast.BroadcastNotificationToUserAsync(userId, BuildRankChangeNotification(oldRank, newRank));
            }

            _lastPointsByUserId = current.ToDictionary(u => u.Id, u => u.Points);
            _lastRankByUserId = current
                .Select((u, i) => (u.Id, Rank: i + 1))
                .ToDictionary(t => t.Id, t => t.Rank);
        }

        private static LiveNotification BuildRankChangeNotification(int oldRank, int newRank)
        {
            bool movedUp = newRank < oldRank;
            return new LiveNotification
            {
                Title = movedUp ? "Rank up!" : "Rank changed",
                Message = movedUp
                    ? $"You moved up to rank {newRank}."
                    : $"You dropped to rank {newRank}."
            };
        }

        // Detects users whose rank shifted between the previous and current snapshots.
        // Only includes users present in BOTH snapshots — newcomers and users who
        // dropped out of the top page are intentionally excluded.
        // topN filters to changes where either oldRank ≤ N or newRank ≤ N, so users
        // who fall out of the top group are still notified.
        public static IEnumerable<(string UserId, int OldRank, int NewRank)> DetectRankChanges(
            IReadOnlyList<ApplicationUser> currentRanked,
            IReadOnlyDictionary<string, int> previousRankByUserId,
            int topN = int.MaxValue)
        {
            for (int i = 0; i < currentRanked.Count; i++)
            {
                var user = currentRanked[i];
                int newRank = i + 1;
                if (previousRankByUserId.TryGetValue(user.Id, out var oldRank) && oldRank != newRank)
                {
                    if (oldRank <= topN || newRank <= topN)
                    {
                        yield return (user.Id, oldRank, newRank);
                    }
                }
            }
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
