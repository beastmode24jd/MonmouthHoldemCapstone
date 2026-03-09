using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{
    [Table("Report")]
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [NotMapped]
        public Guid ReportingUserId
        {
            get => Guid.Parse(ReportingUserIdentityId);
            set => ReportingUserIdentityId = value.ToString();
        }

        [Required]
        [Column("ReportingUserId")]
        [MaxLength(450)]
        [ForeignKey(nameof(Reporter))]
        public string ReportingUserIdentityId { get; set; } = null!;

        [Required]
        [MaxLength(2048)]
        public string ReportedPageUrl { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; } = null;

        [Required]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; } = false;

        public virtual ApplicationUser Reporter { get; set; } = null!;
    }
}