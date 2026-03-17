namespace MH.Capstone.Domain.Constants.Configurables
{
    public class EmailDispatcherOptions
    {
        /// <summary>
        /// Interval in seconds between checks for pending emails.
        /// </summary>
        public int IntervalSeconds { get; set; } = 30;

        /// <summary>
        /// How many emails to process per run.
        /// </summary>
        public int BatchSize { get; set; } = 20;

        /// <summary>
        /// Maximum retry attempts before giving up.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Backoff multiplier in seconds for retries (exponential backoff base).
        /// </summary>
        public int RetryBackoffSeconds { get; set; } = 60;
    }
}
