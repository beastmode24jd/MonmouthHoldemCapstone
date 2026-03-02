using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{

    [Table("Badge")]
    public class Badge
    {
        [Key]
        public Guid BadgeID { get; set; } = Guid.NewGuid();

        // Title and description, for displaying on the frontend later.
        // 150 character maximum.

        [MaxLength(150)]
        public string Description { get; set; } = "";

        [MinLength(1)]
        [MaxLength(50)]
        public string Title { get; set; } = "A"; // Since a length of 1 is required, this is the default value

        // Baseline value of 10 points, for a badge.
        public int PointValue { get; set; } = 10;

        public byte[]? BadgeIcon { get; set; } = null;

    }
}