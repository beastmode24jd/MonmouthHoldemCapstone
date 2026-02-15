using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Sighting")]
    public class Sighting
    {
        [Key]
        public Guid Id { get; set; }

        [NotMapped]
        public Guid UserId
        {
            get => Guid.Parse(UserIdentityId);
            set => UserIdentityId = value.ToString(); // Convert Guid to string for storage in the AspNetCore Identity ID column
        }

        [Column("UserId")]
        [MaxLength(450)] // The size of the UserId column should match the size of the primary key in the AspNetUsers table (nvarchar(450))
        [ForeignKey(nameof(User))]
        public string UserIdentityId { get; set; }

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

        [Length(1, 2 * (1024 * 1024))] // must not be of size 0 but less than 2 MB
        public byte[] ImageBuffer { get; set; } = null!;

        [NotMapped]
        public virtual ApplicationUser User { get; set; } = null!;

        public Sighting() {}

        public Sighting(Guid id, Guid userId, decimal latitude, decimal longitude, DateTime timestamp, 
            string? description, byte[] imageBuffer)
        {
            Id = id;
            UserId = userId;
            Latitude = latitude;
            Longitude = longitude;
            Timestamp = timestamp;
            Description = description;
            ImageBuffer = imageBuffer;
        }
    }
}