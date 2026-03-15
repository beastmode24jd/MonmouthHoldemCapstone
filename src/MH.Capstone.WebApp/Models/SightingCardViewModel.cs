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

       
        public SightingCardViewModel() { }

        // Converts a Sighting entity into a SightingCardViewModel for display.
        // Handles the conversion of the image byte array to a base64 data URL.
        /// <param name="sighting">The sighting entity from the database</param>
        public SightingCardViewModel(Sighting sighting)
        {
            Id = sighting.Id;
            Description = sighting.Description;

            // Convert the byte array to a base64 string and wrap it in a data URL
            // This allows the image to be displayed directly in an <img> tag without a separate endpoint
            // Assuming JPEG format - could be enhanced to detect actual image type from byte header
            var base64 = Convert.ToBase64String(sighting.ImageBuffer);
            ImageDataUrl = $"data:image/jpeg;base64,{base64}";
        }
    }
}
