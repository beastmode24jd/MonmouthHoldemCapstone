using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.WebApp.Models
{
    public class ReportSubmitViewModel
    {
        [Required]
        [MaxLength(2048)]
        public string ReportedPageUrl { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}