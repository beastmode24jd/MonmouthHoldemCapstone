using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{

    [Table("Badges")]
    public class Badge
    {
        [Key]
        public int BadgeID { get; set; }

        // Title and description, for displaying on the frontend later.
        // 150 character maximum.
        [StringLength(150)]
        public string Description { get; set; } = "";

        public string Title { get; set; } = "";

        // Baseline value of 10 points, for a badge.
        public int PointValue { get; set; } = 10;

        public byte[]? BadgeIcon { get; set; }

    }
}