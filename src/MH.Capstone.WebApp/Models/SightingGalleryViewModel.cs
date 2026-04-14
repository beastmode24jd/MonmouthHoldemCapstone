using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{

    // CSP-145: Page-level ViewModel for the Sighting Gallery page.
    // Contains a collection of sighting cards to display in a responsive grid.
    public class SightingGalleryViewModel
    {
        public List<SightingCardViewModel> Sightings { get; set; } = new();

        public bool HasSightings => Sightings.Any();

        public int SightingCount => Sightings.Count;

        // CSP-96: The identity string ID of the logged-in user, used by client-side JS filtering
        public string CurrentUserId { get; set; } = string.Empty;

        public SightingGalleryViewModel() { }

        public SightingGalleryViewModel(IEnumerable<Sighting> sightings, string currentUserId = "")
        {
            Sightings = sightings.Select(s => new SightingCardViewModel(s)).ToList();
            CurrentUserId = currentUserId;
        }
    }
}
