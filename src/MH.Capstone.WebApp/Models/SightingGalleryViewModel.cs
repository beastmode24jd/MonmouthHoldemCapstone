using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.WebApp.Models
{

    // CSP-145: Page-level ViewModel for the Sighting Gallery page.
    // Contains a collection of sighting cards to display in a responsive grid.
    // CSP-199: Now also carries paging metadata so the view can render Prev/Next.
    public class SightingGalleryViewModel
    {
        public List<SightingCardViewModel> Sightings { get; set; } = new();

        public bool HasSightings => Sightings.Any();

        // Count of cards on the CURRENT page. For the dataset total, see TotalCount.
        public int SightingCount => Sightings.Count;

        // CSP-96: The identity string ID of the logged-in user, used by client-side JS filtering
        public string CurrentUserId { get; set; } = string.Empty;

        // CSP-199: paging metadata. Set by the controller from the PagedResult
        // returned by ISightingsService.GetSightingsPageAsync.
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        // Computed flags so the view can show/disable Prev and Next correctly.
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public SightingGalleryViewModel() { }

        // Kept for backwards-compat with any caller still passing a raw sighting list.
        public SightingGalleryViewModel(IEnumerable<Sighting> sightings, string currentUserId = "")
        {
            Sightings = sightings.Select(s => new SightingCardViewModel(s)).ToList();
            CurrentUserId = currentUserId;
        }

        // CSP-199: preferred constructor — wraps a paged service result.
        public SightingGalleryViewModel(PagedResult<Sighting> page, string currentUserId)
        {
            Sightings = page.Items.Select(s => new SightingCardViewModel(s)).ToList();
            CurrentUserId = currentUserId;
            CurrentPage = page.Page;
            PageSize = page.PageSize;
            TotalCount = page.TotalCount;
            TotalPages = page.TotalPages;
        }
    }
}
