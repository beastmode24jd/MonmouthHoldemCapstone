using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Sighting")]
    public class Sighting
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        [Column("Lat")]
        [Range(-90, 90, ErrorMessage = "Latitude must be within -90 and 90 inclusive")]
        [Precision(9,6)] // Maps to DECIMAL(9,6) in the database
        public decimal Latitude { get; set; }

        [Column("Long")]
        [Range(-180, 180, ErrorMessage = "Longitude must be within -180 and 180 inclusive")]
        [Precision(9, 6)] // Maps to DECIMAL(9,6) in the database
        public decimal Longitude { get; set; }

        public DateTime TimeStamp { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; } = null;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}