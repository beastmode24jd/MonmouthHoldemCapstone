using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.Tools;

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
        [MinLength(1)]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(250)] 
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset SentAt { get; set; }

        public bool IsRead { get; set; } = false;

        [NotMapped]
        public bool IsPostdated => SentAt.UtcDateTime > DateTime.UtcNow;

        public virtual ApplicationUser Recipient { get; set; } = null!;

        // EF Core requires a parameterless constructor for materialization of entities from the database,
        // so we need to include one even though we don't want it to be used directly in our code.
        public Notification() {}

        // For testing and general use within the application, we want to require all properties to be set
        // at the time of object creation, so we provide this constructor for that purpose.
        public Notification(Guid id, Guid recipientId, string title, string message, DateTimeOffset sentAt)
        {
            Id = id;
            RecipientId = recipientId;
            Title = title;
            Message = message;
            SentAt = sentAt;
        }
    }
}
