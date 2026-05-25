using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } // PK

        [Required]
        [Column("ActionType")]
        public AuditActionType ActionType { get; set; }

        // The admin who performed the action.
        [Required]
        [Column("PerformingUserId")]
        [MaxLength(450)]
        [ForeignKey(nameof(PerformingUser))]
        public string PerformingUserIdentityId { get; set; } = null!;

        [NotMapped]
        public Guid PerformingUserId
        {
            get => Guid.Parse(PerformingUserIdentityId);
            set => PerformingUserIdentityId = value.ToString();
        }

        // Optional user the action targeted (lock/unlock, promote/demote, etc.).
        [Column("TargetUserId")]
        [MaxLength(450)]
        [ForeignKey(nameof(TargetUser))]
        public string? TargetUserIdentityId { get; set; }

        [NotMapped]
        public Guid? TargetUserId
        {
            get => TargetUserIdentityId is null ? null : Guid.Parse(TargetUserIdentityId);
            set => TargetUserIdentityId = value?.ToString();
        }

        // Optional report the action targeted (resolve/reopen).
        [ForeignKey(nameof(TargetReport))]
        public Guid? TargetReportId { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        [Required]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public virtual ApplicationUser PerformingUser { get; set; } = null!;
        public virtual ApplicationUser? TargetUser { get; set; }
        public virtual Report? TargetReport { get; set; }
    }
}
