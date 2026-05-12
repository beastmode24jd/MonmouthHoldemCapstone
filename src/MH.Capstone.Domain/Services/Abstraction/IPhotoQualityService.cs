using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction;

public interface IPhotoQualityService
{
    // Analyze the given image bytes and return a quality tier along with relevant metadata.
    // CancellatiuonToken is used to cancel long running analysis.
    Task<(PhotoQualityTier Tier, double Sharpness, double Luminance, int Width, int Height)>
        AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken = default);

    // CSP-189: Returns a user-facing message explaining WHY a Low-tier photo failed.
    // Centralizes the reason-decision so the controller doesn't re-classify luminance.
    string GetLowQualityReasonMessage(double sharpness, double luminance);
}
