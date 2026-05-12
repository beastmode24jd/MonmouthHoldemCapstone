using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{
    // CSP-180: Per-user opt-in for real-time leaderboard / scoring notifications.
    // Default is enabled — users explicitly disable via /Dashboard/LiveNotifications.
    [Index(nameof(UserId), IsUnique = true)]
    public class LiveNotificationPreference
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        public bool LiveUpdatesEnabled { get; set; } = true;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
