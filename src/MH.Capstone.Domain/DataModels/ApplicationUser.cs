using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace MH.Capstone.Domain.DataModels
{
    // Represents a user in the application. Extends IdentityUser to include Identity features
    // like password hashing, email confirmation, two-factor auth, etc.
    public class ApplicationUser : IdentityUser
    {
        // Identity already provides: Id, Email, UserName, PasswordHash, etc.
        // Add custom properties if needed:

        // Example: public string? FirstName { get; set; }
        // Example: public string? LastName { get; set; }

        // This property is not mapped to the database, but provides a convenient way
        // to work with the UserId as a Guid instead of a string. Will read and persist
        // to the underlying string Id property of IdentityUser, so this property should
        // always be equivalent to Guid.Parse(Id), including when setting a new id.
        [NotMapped]
        public Guid GuidId
        {
            get => Guid.Parse(Id);
            set => Id = value.ToString();
        }

        // Sets profile icon to default in wwwroot folder if not custom
        public byte[]? ProfileImage { get; set; } = null;

        // The imageType of the profile image (e.g., "image/png", "image/jpeg", etc.)
        [MaxLength(50)]
        public string? ProfileImageType { get; set; } = null;

        public bool IsDeactivated { get; set; } = false;

        // CSP-110 : Total points earned from wildlife sightings and badges
        public int Points { get; set; } = 0;

        public virtual List<Sighting> Sightings { get; set; } = new List<Sighting>();

        public virtual List<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

        [StringLength(250)]
        public string? Bio { get; set; } = null; // Default to null for placeholder value
        
        public virtual List<Notification> Notifications { get; set; } = new List<Notification>();

        public DateTimeOffset? LastLogin { get; set; }
        public int LoginStreak { get; set; } = 0;
    }
}