namespace MH.Capstone.Domain.Services.Abstraction
{
    // CSP-180: Payload pushed to leaderboard clients when a single user's score changes.
    public class LeaderboardEntryUpdate
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Points { get; set; }
        public int Rank { get; set; }
    }
}
