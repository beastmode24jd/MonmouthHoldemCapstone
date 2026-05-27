using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.WebApp.Models
{
    /// <summary>
    /// CSP-202: One sighting inside an expanded Anidex card.
    /// </summary>
    public class AnidexSightingEntryViewModel
    {
        public Guid SightingId { get; set; }
        public string ImageDataUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public AnidexSightingEntryViewModel() { }

        public AnidexSightingEntryViewModel(AnidexSightingEntry entry)
        {
            SightingId = entry.SightingId;
            Description = entry.Description;
            Timestamp = entry.Timestamp;

            if (entry.ImageBuffer is { Length: > 0 })
            {
                var base64 = Convert.ToBase64String(entry.ImageBuffer);
                ImageDataUrl = $"data:image/jpeg;base64,{base64}";
            }
        }
    }

    /// <summary>
    /// CSP-142: One card on the personal Anidex page. Wraps an <see cref="AnidexEntry"/>
    /// from the domain layer and exposes the species photo as a base64 data URL so
    /// the Razor view can render it inline (matches the Sighting Gallery pattern).
    /// CSP-202: <see cref="Entries"/> carries the per-sighting list for the expansion panel.
    /// </summary>
    public class AnidexEntryCardViewModel
    {
        public string SpeciesName { get; set; } = string.Empty;
        public int DiscoveryCount { get; set; }
        public string RarityName { get; set; } = "Common";
        public double RarityMultiplier { get; set; } = 1.0;
        public string ImageDataUrl { get; set; } = string.Empty;
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

            if (entry.LatestImageBuffer is { Length: > 0 })
            {
                var base64 = Convert.ToBase64String(entry.LatestImageBuffer);
                ImageDataUrl = $"data:image/jpeg;base64,{base64}";
            }

            Entries = entry.Entries
                .Select(e => new AnidexSightingEntryViewModel(e))
                .ToList();
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
