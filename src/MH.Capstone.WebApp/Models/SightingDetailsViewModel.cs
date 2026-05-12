using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    /// CSP-172: Detail view for a single sighting. Renders the full image,
    /// uploader attribution, sighting metadata, and a fun fact about the
    /// identified species. When the fun-fact lookup fails or returns nothing,
    /// the controller substitutes a friendly fallback string and sets
    /// <see cref="IsFunFactFallback"/> to true so the view can mark the
    /// element with <c>data-fun-fact-status="fallback"</c>.
    public class SightingDetailsViewModel
    {
        public const string FunFactFallbackMessage =
            "Fun facts about this animal aren't available right now.";

        public Guid Id { get; set; }

        public string ImageDataUrl { get; set; } = string.Empty;

        public string UploaderDisplayName { get; set; } = string.Empty;

        public string UploaderProfileImageDataUrl { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public string SpeciesName { get; set; } = string.Empty;

        public string Rarity { get; set; } = "Common";

        public double RarityMultiplier { get; set; } = 1.0;

        public string FunFact { get; set; } = string.Empty;

        public bool IsFunFactFallback { get; set; }

        // CSP-187: comments section. Author names are resolved by the controller.
        public List<CommentRowViewModel> Comments { get; set; } = new();

        // CSP-187 AC4: true when the viewer can hide / reinstate comments (Admin role).
        public bool ViewerCanModerate { get; set; } = false;

        public SightingDetailsViewModel() { }

        public SightingDetailsViewModel(Sighting sighting, string? funFact)
        {
            Id = sighting.Id;
            Description = sighting.Description;
            Timestamp = sighting.Timestamp;
            Latitude = sighting.Latitude;
            Longitude = sighting.Longitude;
            SpeciesName = sighting.SpeciesName;
            Rarity = sighting.Rarity;
            RarityMultiplier = sighting.RarityMultiplier;

            ImageDataUrl = sighting.ImageBuffer is { Length: > 0 }
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(sighting.ImageBuffer)}"
                : string.Empty;

            var user = sighting.User;
            UploaderDisplayName = user?.DisplayName ?? "Unknown";
            UploaderProfileImageDataUrl =
                user?.ProfileImage is { Length: > 0 } && !string.IsNullOrEmpty(user.ProfileImageType)
                    ? $"data:{user.ProfileImageType};base64,{Convert.ToBase64String(user.ProfileImage)}"
                    : "/imgs/profileDefault.jpg";

            if (string.IsNullOrWhiteSpace(funFact))
            {
                FunFact = FunFactFallbackMessage;
                IsFunFactFallback = true;
            }
            else
            {
                FunFact = funFact;
                IsFunFactFallback = false;
            }
        }
    }
}
