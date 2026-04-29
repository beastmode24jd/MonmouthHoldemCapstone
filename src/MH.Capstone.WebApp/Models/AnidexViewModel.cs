using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.WebApp.Models
{
    /// <summary>
    /// CSP-142: One card on the personal Anidex page. Wraps an <see cref="AnidexEntry"/>
    /// from the domain layer and exposes the species photo as a base64 data URL so
    /// the Razor view can render it inline (matches the Sighting Gallery pattern).
    /// </summary>
    public class AnidexEntryCardViewModel
    {
        public string SpeciesName { get; set; } = string.Empty;
        public int DiscoveryCount { get; set; }
        public string RarityName { get; set; } = "Common";
        public double RarityMultiplier { get; set; } = 1.0;
        public string ImageDataUrl { get; set; } = string.Empty;
        public DateTimeOffset LatestSightingTimestamp { get; set; }

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
