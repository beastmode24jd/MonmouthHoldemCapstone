using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Sighting")]
    public partial class Sighting
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        [Column("Lat")]
        [Precision(9,6)] // Maps to DECIMAL(9,6) in the database
        public decimal Latitude { get; set; }

        [Column("Long")]
        [Precision(9, 6)] // Maps to DECIMAL(9,6) in the database
        public decimal Longitude { get; set; }

        public DateTime TimeStamp { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; } = null;

        // Commented out until User is defined by other developers
        //[ForeignKey(nameof(UserId))]
        //public virtual User User { get; set; } = null!;
    }
}
