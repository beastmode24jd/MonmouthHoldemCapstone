using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    public enum CommentModerationAction
    {
        Hidden = 0,
        Reinstated = 1,
    }

    [Table("CommentModerationLog")]
    public class CommentModerationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Comment))]
        public Guid CommentId { get; set; }

        [NotMapped]
        public Guid ModeratorId
        {
            get => Guid.Parse(ModeratorIdentityId);
            set => ModeratorIdentityId = value.ToString();
        }

        [Required]
        [Column("ModeratorId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Moderator))]
        public string ModeratorIdentityId { get; set; } = null!;

        [Required]
        public CommentModerationAction Action { get; set; }

        [MaxLength(200)]
        public string? Reason { get; set; } = null;

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual Comment Comment { get; set; } = null!;
        public virtual ApplicationUser Moderator { get; set; } = null!;

        public CommentModerationLog() { }
    }
}
