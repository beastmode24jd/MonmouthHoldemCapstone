using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{

    // CSP-145: Page-level ViewModel for the Sighting Gallery page.
    // Contains a collection of sighting cards to display in a responsive grid.
    public class SightingGalleryViewModel
    {
        
        // Collection of sighting cards to display in the gallery.
        // Each card represents one sighting uploaded by the user.
        public List<SightingCardViewModel> Sightings { get; set; } = new();

      
        // Check if the user has any sightings.
        // if false then display "You have no sightings yet. Start uploading to earn points and badges!" instead of the gallery grid.
        public bool HasSightings => Sightings.Any();

        
        // Display total count of sightings on card  ("You have 12 sightings")
        public int SightingCount => Sightings.Count;

       
        public SightingGalleryViewModel() { }

      
        // Creates a SightingGalleryViewModel from a collection of Sighting entities.
        // Converts each Sighting into a SightingCardViewModel for display.
        /// <param name="sightings">Collection of sighting entities from the database</param>
        public SightingGalleryViewModel(IEnumerable<Sighting> sightings)
        {
            // Convert each Sighting entity to a SightingCardViewModel
            // This handles the byte[] to base64 conversion and formatting
            Sightings = sightings.Select(s => new SightingCardViewModel(s)).ToList();
        }
    }
}
