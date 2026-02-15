using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.Tools;
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

        [PastDateTime]
        public DateTime Timestamp { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; } = null;

        // Commented out until User is defined by other developers
        //[ForeignKey(nameof(UserId))]
        //public virtual User User { get; set; } = null!;

        public Sighting() {}

        public Sighting(Guid id, Guid userId, decimal latitude, decimal longitude, DateTime timestamp, string? description)
        {
            Id = id;
            UserId = userId;
            Latitude = latitude;
            Longitude = longitude;
            Timestamp = timestamp;
            Description = description;
        }
    }
}