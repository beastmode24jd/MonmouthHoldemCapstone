using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
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
            get => Guid.Parse(LinkedUserIdentityId);
            set => LinkedUserIdentityId =
                value.ToString(); // Convert Guid to string for storage in the AspNetCore Identity ID column
        }

        [Required]
        [Column("RecipientId")]
        [MaxLength(450)] // The size of the RecipientId column should match the size of the primary key in the AspNetUsers table (nvarchar(450))
        [ForeignKey(nameof(Recipient))]
        public string LinkedUserIdentityId { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        [MaxLength(250)]
        public string Message { get; set; } = string.Empty;

        [Required] public DateTimeOffset SentAt { get; set; }

        public bool IsRead { get; set; } = false;

        [NotMapped] public bool IsPostdated => SentAt.UtcDateTime > DateTime.UtcNow;

        public virtual ApplicationUser Recipient { get; set; } = null!;

        // EF Core requires a parameterless constructor for materialization of entities from the database,
        // so we need to include one even though we don't want it to be used directly in our code.
        public Notification()
        {
        }

        // For general use within the application, we want to require all properties to be set at the time of object creation,
        // so we provide this constructor for that purpose.
        public Notification(Guid recipientId, string title, string message, DateTimeOffset sentAt)
        {
            RecipientId = recipientId;
            Title = title;
            Message = message;
            SentAt = sentAt;
        }

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

        /// <summary>
        /// Sends a notification to a recipient (<see cref="ApplicationUser"/>) with the specified title and message,
        /// and an optional sentAt timestamp if not sent at the current time and date.
        /// </summary>
        /// <param name="recipientId">The id (<see cref="Guid"/>) of the recipient (<see cref="ApplicationUser"/>) to send the notification to</param>
        /// <param name="title">The message of 50 characters or fewer for the notification</param>
        /// <param name="message">The message of 250 characters or fewer for the notification</param>
        /// <param name="sentAt">Timestamp of when the Notification was sent. Can be postdated (in the future).
        /// A null value defaults to the current time</param>
        /// <returns></returns>
        public static Notification Create(Guid recipientId, string title, string message, DateTimeOffset? sentAt = null)
        {
            sentAt ??= DateTimeOffset.UtcNow;
            return new Notification(recipientId, title, message, (DateTimeOffset)sentAt);
        }
    }
}
