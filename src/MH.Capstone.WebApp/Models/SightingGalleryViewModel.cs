using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    /// <summary>
    /// CSP-145: Page-level ViewModel for the Sighting Gallery page.
    /// Contains a collection of sighting cards to display in a responsive grid.
    /// </summary>
    public class SightingGalleryViewModel
    {
        /// <summary>
        /// Collection of sighting cards to display in the gallery.
        /// Each card represents one sighting uploaded by the user.
        /// </summary>
        public List<SightingCardViewModel> Sightings { get; set; } = new();

        /// <summary>
        /// Computed property to check if the user has any sightings.
        /// Used to determine whether to show the gallery grid or the empty state message.
        /// </summary>
        public bool HasSightings => Sightings.Any();

        /// <summary>
        /// Total count of sightings for display (e.g., "You have 12 sightings")
        /// </summary>
        public int SightingCount => Sightings.Count;

        /// <summary>
        /// Parameterless constructor for ASP.NET Core model binding
        /// </summary>
        public SightingGalleryViewModel() { }

        /// <summary>
        /// Creates a SightingGalleryViewModel from a collection of Sighting entities.
        /// Converts each Sighting into a SightingCardViewModel for display.
        /// </summary>
        /// <param name="sightings">Collection of sighting entities from the database</param>
        public SightingGalleryViewModel(IEnumerable<Sighting> sightings)
        {
            // Convert each Sighting entity to a SightingCardViewModel
            // This handles the byte[] to base64 conversion and formatting
            Sightings = sightings.Select(s => new SightingCardViewModel(s)).ToList();
        }
    }
}
