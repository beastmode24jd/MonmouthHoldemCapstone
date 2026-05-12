namespace MH.Capstone.Domain.Services.Abstraction
{
    // CSP-180: Pushes real-time updates to connected clients. Implementation
    // wraps a SignalR IHubContext in the WebApp; Domain code only sees this contract.
    public interface ILiveBroadcastService
    {
        // Broadcasts a leaderboard score change to all connected clients.
        Task BroadcastLeaderboardUpdateAsync(LeaderboardEntryUpdate update);

        // Sends a live in-app notification to a specific user.
        // Honors the user's live-notification preference: if opted out, sends nothing.
        Task BroadcastNotificationToUserAsync(string userId, LiveNotification notification);
    }
}
