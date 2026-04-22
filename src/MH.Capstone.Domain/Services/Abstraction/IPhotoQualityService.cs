using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction;

public interface IPhotoQualityService
{
    // Analyze the given image bytes and return a quality tier along with relevant metadata.
    // CancellatiuonToken is used to cancel long running analysis.
    Task<(PhotoQualityTier Tier, double Sharpness, double Luminance, int Width, int Height)>
        AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
}
