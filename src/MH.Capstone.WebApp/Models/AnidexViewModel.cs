using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.WebApp.Models
{
    /// <summary>
    /// CSP-202: One sighting inside an expanded Anidex card.
    /// CSP-277: Carries only the Sighting id; the photo is fetched lazily from
    /// <c>GET /anidex/image/{id}</c> instead of being base64-inlined into the page.
    /// </summary>
    public class AnidexSightingEntryViewModel
    {
        public Guid SightingId { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public AnidexSightingEntryViewModel() { }

        public AnidexSightingEntryViewModel(AnidexSightingEntry entry)
        {
            SightingId = entry.SightingId;
            Description = entry.Description;
            Timestamp = entry.Timestamp;
        }
    }

    /// <summary>
    /// CSP-142: One card on the personal Anidex page. Wraps an <see cref="AnidexEntry"/>
    /// from the domain layer.
    /// CSP-202: <see cref="Entries"/> carries the per-sighting list for the expansion panel.
    /// CSP-277: The card thumbnail and every expansion photo are now served from
    /// <c>GET /anidex/image/{id}</c> and loaded lazily, rather than base64-inlined into the
    /// HTML. <see cref="LatestSightingId"/> is the newest sighting (its photo is the card face).
    /// </summary>
    public class AnidexEntryCardViewModel
    {
        public string SpeciesName { get; set; } = string.Empty;
        public int DiscoveryCount { get; set; }
        public string RarityName { get; set; } = "Common";
        public double RarityMultiplier { get; set; } = 1.0;
        public Guid? LatestSightingId { get; set; }
        public DateTimeOffset LatestSightingTimestamp { get; set; }
        public IReadOnlyList<AnidexSightingEntryViewModel> Entries { get; set; } = [];

        public bool OffersExpansion => DiscoveryCount > 1;

        public AnidexEntryCardViewModel() { }

        public AnidexEntryCardViewModel(AnidexEntry entry)
        {
            SpeciesName = entry.SpeciesName;
            DiscoveryCount = entry.DiscoveryCount;
            RarityName = entry.RarityName;
            RarityMultiplier = entry.RarityMultiplier;
            LatestSightingTimestamp = entry.LatestSightingTimestamp;

            Entries = entry.Entries
                .Select(e => new AnidexSightingEntryViewModel(e))
                .ToList();

            // Entries are newest-first (see SightingsService.GetUserAnidexAsync), so the
            // first entry is the same sighting whose photo is the card thumbnail.
            LatestSightingId = Entries.Count > 0 ? Entries[0].SightingId : null;
        }
    }

    /// <summary>
    /// CSP-142: Anidex page model — collection of species the authenticated user has confirmed.
    /// </summary>
    public class AnidexViewModel
    {
        public IReadOnlyList<AnidexEntryCardViewModel> Entries { get; }
        public bool IsEmpty => Entries.Count == 0;
        public int TotalSpecies => Entries.Count;

        public AnidexViewModel(IEnumerable<AnidexEntry> entries)
        {
            Entries = entries.Select(e => new AnidexEntryCardViewModel(e)).ToList();
        }
    }
}
