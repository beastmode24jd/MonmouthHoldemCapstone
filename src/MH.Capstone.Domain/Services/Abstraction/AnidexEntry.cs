namespace MH.Capstone.Domain.Services.Abstraction;

/// <summary>
/// CSP-142: One row of a user's personal Anidex collection.
/// Each entry represents a unique species the user has confirmed via their sightings.
/// </summary>
/// <param name="SpeciesName">Canonical species name from Sighting.SpeciesName.</param>
/// <param name="DiscoveryCount">How many of the user's sightings match this species.</param>
/// <param name="RarityName">"Mythic" / "Rare" / "Common" — derived from the GLOBAL sighting count for this species, not this user's.</param>
/// <param name="RarityMultiplier">Numeric multiplier paired with RarityName (5.0 / 2.0 / 1.0).</param>
/// <param name="LatestImageBuffer">Image bytes of the user's most recent sighting of this species — used as the gallery thumbnail.</param>
/// <param name="LatestSightingTimestamp">Timestamp of the most recent sighting of this species by the user.</param>
public record AnidexEntry(
    string SpeciesName,
    int DiscoveryCount,
    string RarityName,
    double RarityMultiplier,
    byte[] LatestImageBuffer,
    DateTimeOffset LatestSightingTimestamp);
