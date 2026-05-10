using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Comment")]
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Sighting))]
        public Guid SightingId { get; set; }

        [NotMapped]
        public Guid AuthorId
        {
            get => Guid.Parse(AuthorIdentityId);
            set => AuthorIdentityId = value.ToString();
        }

        [Required]
        [Column("AuthorId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Author))]
        public string AuthorIdentityId { get; set; } = null!;

        [Required]
        [MinLength(1)]
        [MaxLength(1000)]
        public string Body { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public bool IsHidden { get; set; } = false;

        public DateTimeOffset? HiddenAt { get; set; } = null;

        [MaxLength(450)]
        public string? HiddenByIdentityId { get; set; } = null;

        [MaxLength(200)]
        public string? HiddenReason { get; set; } = null;

        public virtual Sighting Sighting { get; set; } = null!;
        public virtual ApplicationUser Author { get; set; } = null!;

        public Comment() { }

        public Comment(Guid sightingId, Guid authorId, string body)
        {
            SightingId = sightingId;
            AuthorId = authorId;
            Body = body;
        }
    }
}
