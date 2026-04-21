using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Club")]
    public class Club
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; } = null;

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [NotMapped]
        public Guid OwnerId
        {
            get => Guid.Parse(OwnerIdentityId);
            set => OwnerIdentityId = value.ToString();
        }

        [Required]
        [Column("OwnerId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Owner))]
        public string OwnerIdentityId { get; set; } = null!;

        public virtual ApplicationUser Owner { get; set; } = null!;

        public virtual List<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();

        public virtual List<Message> Messages { get; set; } = new List<Message>();

        public Club() { }

        public Club(Guid ownerId, string name, string? description, DateTimeOffset createdAt)
        {
            OwnerId = ownerId;
            Name = name;
            Description = description;
            CreatedAt = createdAt;
        }

        /* IMPORTANT FUTURE CLUB SERVICE FILE NOTE!!!

            "Deleting a user will now throw if they still have club memberships or messages.
            Your service layer will need to clean those up before deleting a user."

        */
    }
}
