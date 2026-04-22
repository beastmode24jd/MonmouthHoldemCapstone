using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MH.Capstone.Domain.Services;

// Analyzes uploaded photos for sharpness, luminance, and resolution using
// SixLabors.ImageSharp. Implements CSP-122 Photo Quality Gate.
public class PhotoQualityService : IPhotoQualityService
{
    public Task<(PhotoQualityTier Tier, double Sharpness, double Luminance, int Width, int Height)>
        AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes must be non-null and non-empty.", nameof(imageBytes));



        using var image = Image.Load<Rgba32>(imageBytes);

        // Placeholder values until later TDD cycles add sharpness, luminance, and tier logic.
        var result = (
            Tier:      PhotoQualityTier.Unknown,
            Sharpness: 0.0,
            Luminance: 0.0,
            Width:     image.Width,
            Height:    image.Height);

        return Task.FromResult(result);
    }
}
