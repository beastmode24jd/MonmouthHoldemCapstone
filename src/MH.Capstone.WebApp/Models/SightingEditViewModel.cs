using System.ComponentModel.DataAnnotations;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    /// CSP-37: Edit form for an existing sighting. Only Description and SpeciesName are
    /// editable — GPS coordinates, timestamp, and photo are carried as read-only context
    /// and shown but not posted back. Description is REQUIRED here (stricter than the upload
    /// form, where it is optional) per the ticket's "non-empty description" rule; this is a
    /// presentation-layer rule, the Sighting entity still allows null descriptions.
    public class SightingEditViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Please provide a description.")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please name the species.")]
        [MaxLength(100)]
        [Display(Name = "Species")]
        public string SpeciesName { get; set; } = string.Empty;

        // --- Read-only context (display only; never edited or persisted from this form) ---

        public string ImageDataUrl { get; set; } = string.Empty;

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public SightingEditViewModel() { }

        public SightingEditViewModel(Sighting sighting)
        {
            Id = sighting.Id;
            Description = sighting.Description ?? string.Empty;
            SpeciesName = sighting.SpeciesName;
            Latitude = sighting.Latitude;
            Longitude = sighting.Longitude;
            Timestamp = sighting.Timestamp;
            ImageDataUrl = sighting.ImageBuffer is { Length: > 0 }
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(sighting.ImageBuffer)}"
                : string.Empty;
        }
    }
}
