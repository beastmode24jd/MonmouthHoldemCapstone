using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("UserNotificationPreference")]
    public class UserNotificationPreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(450)]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;

        [Required]
        public NotificationType NotificationType { get; set; }

        [Required]
        public NotificationDeliveryChannel DeliveryChannel { get; set; } = NotificationDeliveryChannel.InAppOnly;

        public virtual ApplicationUser User { get; set; } = null!;

        public UserNotificationPreference() { }

        public UserNotificationPreference(string userId, NotificationType notificationType, NotificationDeliveryChannel deliveryChannel)
        {
            UserId = userId;
            NotificationType = notificationType;
            DeliveryChannel = deliveryChannel;
        }
    }
}
