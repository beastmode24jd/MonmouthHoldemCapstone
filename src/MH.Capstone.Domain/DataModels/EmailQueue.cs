using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;

namespace MH.Capstone.Domain.DataModels
{
    [Table("EmailQueue")]
    public class EmailQueue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string Recipient { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string HtmlBody { get; set; } = string.Empty;

        public string? PlainTextBody { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ScheduledAt { get; set; }

        public bool IsSent { get; set; } = false;

        public DateTimeOffset? SentAt { get; set; }

        public int Attempts { get; set; } = 0;

        public DateTimeOffset? LastAttemptAt { get; set; }

        public string? LastError { get; set; }

        // If the dispatcher is currently processing this message; helps prevent local double processing
        public bool Processing { get; set; } = false;

        /// <summary>
        /// Enqueue this EmailQueue entry using the provided repository.
        /// This method delegates to the project's SaveModelAsync extension which calls AddOrUpdateAsync on the repository.
        /// </summary>
        public Task EnqueueAsync(IRepository<EmailQueue, ApplicationDbContext> repo)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            return this.SaveModelAsync(repo);
        }
    }
}
