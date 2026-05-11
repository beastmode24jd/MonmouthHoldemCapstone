using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("UserFollow")]
    public class UserFollow
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [NotMapped]
        public Guid FollowerId
        {
            get => Guid.Parse(FollowerIdentityId);
            set => FollowerIdentityId = value.ToString();
        }

        [Required]
        [Column("FollowerId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Follower))]
        public string FollowerIdentityId { get; set; } = null!;

        [NotMapped]
        public Guid FolloweeId
        {
            get => Guid.Parse(FolloweeIdentityId);
            set => FolloweeIdentityId = value.ToString();
        }

        [Required]
        [Column("FolloweeId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Followee))]
        public string FolloweeIdentityId { get; set; } = null!;

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ApplicationUser Follower { get; set; } = null!;
        public virtual ApplicationUser Followee { get; set; } = null!;

        public UserFollow() { }

        public UserFollow(Guid followerId, Guid followeeId)
        {
            FollowerId = followerId;
            FolloweeId = followeeId;
        }
    }
}
