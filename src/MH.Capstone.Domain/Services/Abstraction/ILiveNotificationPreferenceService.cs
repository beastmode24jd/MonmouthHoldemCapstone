namespace MH.Capstone.Domain.Services.Abstraction
{
    // CSP-180: Per-user opt-in for real-time leaderboard / scoring notifications.
    public interface ILiveNotificationPreferenceService
    {
        // Returns true when the user has live updates enabled.
        // Defaults to true when no preference row exists.
        Task<bool> IsEnabledAsync(string userId);

        // Inserts or updates the user's preference.
        Task SetEnabledAsync(string userId, bool enabled);
    }
}
