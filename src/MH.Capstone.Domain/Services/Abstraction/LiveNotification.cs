namespace MH.Capstone.Domain.Services.Abstraction
{
    // CSP-180: Generic in-app live notification payload (toasts, alerts).
    public class LiveNotification
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
    }
}
