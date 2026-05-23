using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Audit Logs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } // PK

        [Required]
        [Column("Action Type")]
        public AuditActionType ActionType { get; set; }

        [Required]
        [Column("PerformingUser Id")]
        [MaxLength(450)]
        [ForeignKey(nameof(ApplicationUser))]
        public string PerformingUserIdentityId { get; set; } = null!;

        [NotMapped]
        public Guid PerformingUserId
        {
            get => Guid.Parse(PerformingUserIdentityId);
            set => PerformingUserIdentityId = value.ToString();
        }

        [NotMapped]
        public Guid TargetUserId
        {
            get => Guid.Parse(TargetUserIdentityId);
            set => TargetUserIdentityId = value.ToString();
        }

        [Required]
        [Column("TargetUser Id")]
        [MaxLength(450)]
        [ForeignKey(nameof(TargetUser))]
        public string TargetUserIdentityId { get; set; } = null!; // FK

        [MaxLength(1000)]
        public string? Details { get; set; }

        [Required]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public virtual ApplicationUser TargetUser { get; set; } = null!;

    }
}