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
        [Precision(9, 6)]
        public decimal Latitude { get; set; }

        [Column("Long")]
        [Precision(9, 6)]
        public decimal Longitude { get; set; }

        public DateTime TimeStamp { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; } = null;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}