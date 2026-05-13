namespace MH.Capstone.Domain.Tools;

public class LeaderboardWatcherOptions
{
    public const string Section = "LeaderboardWatcher";

    /// <summary>Hours between rank-change sweep ticks. Default: 24. Supports decimals (e.g. 0.5 = 30 min).</summary>
    public double PollIntervalHours { get; set; } = 24;
}
