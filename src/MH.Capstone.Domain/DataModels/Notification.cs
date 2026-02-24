using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Notification")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [NotMapped]
        public Guid RecipientId
        {
            get => Guid.Parse(RecipientIdentityId);
            set => RecipientIdentityId = value.ToString(); // Convert Guid to string for storage in the AspNetCore Identity ID column
        }

        [Required]
        [Column("RecipientId")]
        [MaxLength(450)] // The size of the RecipientId column should match the size of the primary key in the AspNetUsers table (nvarchar(450))
        [ForeignKey(nameof(Recipient))]
        public string RecipientIdentityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [Required] 
        [MaxLength(250)] 
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset SentAt { get; set; }

        public bool IsRead { get; set; } = false;

        [NotMapped]
        public bool IsPostdated => SentAt.UtcDateTime > DateTime.UtcNow;

        public virtual ApplicationUser Recipient { get; set; } = null!;
    }
}
