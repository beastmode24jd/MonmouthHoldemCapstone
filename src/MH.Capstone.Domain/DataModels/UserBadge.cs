using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{

    [Table("PersonalBadges")]
    public class UserBadge
    {
        /* Holds user-specific badge metadata in a join table,
            so baseline badge data is kept separate. */

        // Primary key
        [Key]
        public Guid UserBadgeId { get; set; }

        // Foreign Key for the UserID
        // It's a string due to ApplicationUser inheriting the field from IdentityUser
        [Column("User ID")]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [Column("Badge ID")]
        public Guid BadgeId { get; set; }

        [ForeignKey(nameof(BadgeId))]
        public virtual Badge Badge { get; set; } = null!;

        // Timestamp for when the user earned the badge
        public DateTimeOffset? BadgeEarned { get; set; } = null;

        /*
        [Column("Badge Progress")]
        public int BadgeProgress { get; set; } = 0;
        // Tracks the "progress" of a Badge from the User
        //      (ex. 2/25 Sightings)
        */

    }

}