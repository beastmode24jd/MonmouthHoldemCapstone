using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Tests.Acceptance.Support;

// Deterministic photo quality stub for acceptance tests.
// Returns medium-tier results with acceptable resolution so the CSP-122
// quality gate (≥ 1024 px long side) never rejects test uploads.
[ExcludeFromCodeCoverage]
public class TestPhotoQualityService : IPhotoQualityService
{
    public Task<(PhotoQualityTier Tier, double Sharpness, double Luminance, int Width, int Height)>
        AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((PhotoQualityTier.Medium, 200.0, 0.5, 1200, 900));
    }
}
