namespace MH.Capstone.Domain.Services.Abstraction;

/// <summary>
/// Result of an AI image-recognition call (CSP-144).
/// </summary>
/// <param name="Species">The common name of the identified species, or "Unknown" when not identified.</param>
/// <param name="Description">A short 1-2 sentence description of what the AI sees in the photo.</param>
/// <param name="Identified">True when the AI returned a confident species identification; false for non-wildlife or ambiguous photos.</param>
public record AiIdentificationResult(string Species, string Description, bool Identified);
