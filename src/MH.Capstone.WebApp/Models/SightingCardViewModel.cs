using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    /// CSP-145: Represents a single sighting card in the gallery view.
    /// Converts the Sighting entity's byte[] ImageBuffer into a displayable format and formats the data for presentation in the view.
    
    public class SightingCardViewModel
    {
        public Guid Id { get; set; }

        public string ImageDataUrl { get; set; } = string.Empty;

        public string? Description { get; set; }

        // CSP-96: Attribution — the identity string ID of the submitting user
        public string SubmittedByUserId { get; set; } = string.Empty;

        // CSP-96: Attribution — the display username of the submitting user
        public string SubmittedByUsername { get; set; } = string.Empty;

        // New metadata fields, added from Sighting data model

        public int PointValue { get; set; } = 10;

        public bool LoginStreak { get; set; }

        public string Rarity { get; set; } = "Common";

        public double RarityMultiplier { get; set; } = 1.0;

        public DateTimeOffset Timestamp { get; set; }

       
        public SightingCardViewModel() { }

        public SightingCardViewModel(Sighting sighting)
        {
            Id = sighting.Id;
            Description = sighting.Description;
            SubmittedByUserId = sighting.UserIdentityId;
            SubmittedByUsername = sighting.User?.UserName ?? "Unknown";
            PointValue = sighting.PointValue;
            LoginStreak = sighting.LoginStreak;
            Rarity = sighting.Rarity;
            RarityMultiplier = sighting.RarityMultiplier;
            Timestamp = sighting.Timestamp;

            var base64 = Convert.ToBase64String(sighting.ImageBuffer);
            ImageDataUrl = $"data:image/jpeg;base64,{base64}";


        }
    }
}
