namespace MH.Capstone.Domain.Services.Abstraction;

/// <summary>
/// CSP-142: One row of a user's personal Anidex collection.
/// Each entry represents a unique species the user has confirmed via their sightings.
/// CSP-202: <see cref="Entries"/> preloads every per-species sighting (newest-first)
/// so the UI can expand a card without an extra round-trip.
/// </summary>
/// <param name="SpeciesName">Canonical species name from Sighting.SpeciesName.</param>
/// <param name="DiscoveryCount">How many of the user's sightings match this species.</param>
/// <param name="RarityName">"Mythic" / "Rare" / "Common" — derived from the GLOBAL sighting count for this species, not this user's.</param>
/// <param name="RarityMultiplier">Numeric multiplier paired with RarityName (5.0 / 2.0 / 1.0).</param>
/// <param name="LatestImageBuffer">Image bytes of the user's most recent sighting of this species — used as the gallery thumbnail.</param>
/// <param name="LatestSightingTimestamp">Timestamp of the most recent sighting of this species by the user.</param>
/// <param name="Entries">Every user-owned sighting under this species, newest-first.</param>
public record AnidexEntry(
    string SpeciesName,
    int DiscoveryCount,
    string RarityName,
    double RarityMultiplier,
    byte[] LatestImageBuffer,
    DateTimeOffset LatestSightingTimestamp,
    IReadOnlyList<AnidexSightingEntry> Entries);

/// <summary>
/// CSP-202: One sighting inside an <see cref="AnidexEntry"/>'s expansion list.
/// </summary>
/// <param name="SightingId">The underlying Sighting.Id (stable, used as DOM key).</param>
/// <param name="ImageBuffer">Per-entry photo bytes.</param>
/// <param name="Description">Per-entry short description, nullable matching Sighting.Description.</param>
/// <param name="Timestamp">When the user logged this sighting.</param>
public record AnidexSightingEntry(
    Guid SightingId,
    byte[] ImageBuffer,
    string? Description,
    DateTimeOffset Timestamp);
