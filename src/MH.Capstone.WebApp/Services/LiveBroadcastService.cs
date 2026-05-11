using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MH.Capstone.WebApp.Services
{
    public class LiveBroadcastService : ILiveBroadcastService
    {
        public const string LeaderboardUpdatedEvent = "LeaderboardUpdated";
        public const string LiveNotificationEvent = "LiveNotification";

        private readonly IHubContext<LeaderboardHub> _hubContext;
        private readonly ILiveNotificationPreferenceService _preferences;

        public LiveBroadcastService(
            IHubContext<LeaderboardHub> hubContext,
            ILiveNotificationPreferenceService preferences)
        {
            _hubContext = hubContext;
            _preferences = preferences;
        }

        public Task BroadcastLeaderboardUpdateAsync(LeaderboardEntryUpdate update)
        {
            ArgumentNullException.ThrowIfNull(update);
            return _hubContext.Clients.All.SendAsync(LeaderboardUpdatedEvent, update);
        }

        public async Task BroadcastNotificationToUserAsync(string userId, LiveNotification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required", nameof(userId));

            var enabled = await _preferences.IsEnabledAsync(userId);
            if (!enabled) return;

            await _hubContext.Clients.User(userId).SendAsync(LiveNotificationEvent, notification);
        }
    }
}
