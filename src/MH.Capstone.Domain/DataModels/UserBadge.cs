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
        public int UserBadgeId { get; set; }

        // Foreign Key for the UserID
        public string UserId { get; set; } = "";
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        // Foreign Key for the badgeID
        public int BadgeId { get; set; }

        // Inherits
        [ForeignKey("BadgeId")]
        public virtual Badge Badge { get; set; } = null!;

        // Timestamp for when the user earned the badge
        public DateTime? BadgeEarned { get; set; } = null;

    }

}