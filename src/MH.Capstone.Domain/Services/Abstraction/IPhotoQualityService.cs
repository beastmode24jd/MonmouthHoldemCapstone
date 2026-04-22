using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction;

// Service that analyzes a photo's sharpness, luminance, and resolution,
// returning a quality tier plus the raw measurements. See CSP-122.
public interface IPhotoQualityService
{
    // Analyze the given image bytes and return the computed quality values.
    // Returns Tier = Unknown on analysis failure (corrupt bytes, unsupported format, etc.).
    Task<(PhotoQualityTier Tier, double Sharpness, double Luminance, int Width, int Height)>
        AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
}
