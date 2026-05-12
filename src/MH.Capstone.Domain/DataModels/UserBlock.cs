using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("UserBlock")]
    public class UserBlock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [NotMapped]
        public Guid BlockerId
        {
            get => Guid.Parse(BlockerIdentityId);
            set => BlockerIdentityId = value.ToString();
        }

        [Required]
        [Column("BlockerId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Blocker))]
        public string BlockerIdentityId { get; set; } = null!;

        [NotMapped]
        public Guid BlockedId
        {
            get => Guid.Parse(BlockedIdentityId);
            set => BlockedIdentityId = value.ToString();
        }

        [Required]
        [Column("BlockedId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Blocked))]
        public string BlockedIdentityId { get; set; } = null!;

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ApplicationUser Blocker { get; set; } = null!;
        public virtual ApplicationUser Blocked { get; set; } = null!;

        public UserBlock() { }

        public UserBlock(Guid blockerId, Guid blockedId)
        {
            BlockerId = blockerId;
            BlockedId = blockedId;
        }
    }
}
