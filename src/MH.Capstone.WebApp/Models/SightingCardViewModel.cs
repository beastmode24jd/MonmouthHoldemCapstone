using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    /// CSP-145: Represents a single sighting card in the gallery view.
    /// Converts the Sighting entity's byte[] ImageBuffer into a displayable format and formats the data for presentation in the view.
    
    public class SightingCardViewModel
    {

        
        public Guid Id { get; set; }

    
        public string ImageDataUrl { get; set; } = string.Empty;

        
        /// Optional description of the sighting provided by the user
        public string? Description { get; set; }

        // New metadata fields, added from Sighting data model

        public int PointValue { get; set; } = 10;

        public bool LoginStreak { get; set; }

        public string Rarity { get; set; } = "Common";

        public double RarityMultiplier { get; set; } = 1.0;

        public DateTimeOffset Timestamp { get; set; }

       
        public SightingCardViewModel() { }

        // Converts a Sighting entity into a SightingCardViewModel for display.
        // Handles the conversion of the image byte array to a base64 data URL.
        /// <param name="sighting">The sighting entity from the database</param>
        public SightingCardViewModel(Sighting sighting)
        {
            Id = sighting.Id;
            Description = sighting.Description;
            PointValue = sighting.PointValue;
            LoginStreak = sighting.LoginStreak;
            Rarity = sighting.Rarity;
            RarityMultiplier = sighting.RarityMultiplier;
            Timestamp = sighting.Timestamp;

            // Convert the byte array to a base64 string and wrap it in a data URL
            // This allows the image to be displayed directly in an <img> tag without a separate endpoint
            // Assuming JPEG format - could be enhanced to detect actual image type from byte header
            var base64 = Convert.ToBase64String(sighting.ImageBuffer);
            ImageDataUrl = $"data:image/jpeg;base64,{base64}";


        }
    }
}
