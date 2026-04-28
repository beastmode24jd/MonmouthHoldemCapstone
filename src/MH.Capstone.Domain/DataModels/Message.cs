using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Message")]
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Club))]
        public Guid ClubId { get; set; }

        public virtual Club Club { get; set; } = null!;

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

        public virtual ApplicationUser Author { get; set; } = null!;

        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset SentAt { get; set; }

        public Message() { }

        public Message(Guid clubId, Guid authorId, string content, DateTimeOffset sentAt)
        {
            ClubId = clubId;
            AuthorId = authorId;
            Content = content;
            SentAt = sentAt;
        }
    }
}
