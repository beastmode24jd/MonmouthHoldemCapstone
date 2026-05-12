using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MH.Capstone.Domain.Services;

// Analyzes uploaded photos for sharpness, luminance, and resolution using
// SixLabors.ImageSharp. Implements CSP-122 Photo Quality Gate.
public class PhotoQualityService : IPhotoQualityService
{
    // Thresholds taken directly from the CSP-122 spec.
    private const double BlurThreshold = 100.0;
    private const double SharpThreshold = 300.0;
    private const double DarkLuminanceThreshold = 0.20;
    private const double WashedOutLuminanceThreshold = 0.85;
    private const double HighTierLuminanceMin = 0.30;
    private const double HighTierLuminanceMax = 0.75;
    private const int HighTierLongSidePixels = 2048;

    public Task<(PhotoQualityTier Tier, double Sharpness, double Luminance, int Width, int Height)>
        AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes must be non-null and non-empty.", nameof(imageBytes));

        using var image = Image.Load<Rgba32>(imageBytes);

        int width = image.Width;
        int height = image.Height;

        // Single-pass: build a normalized [0,1] grayscale buffer (Rec.709 luma) and
        // accumulate the luminance sum.
        var gray = new float[width * height];
        double luminanceSum = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = accessor.GetRowSpan(y);
                int rowOffset = y * width;
                for (int x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    float luma = (0.2126f * p.R + 0.7152f * p.G + 0.0722f * p.B) / 255f;
                    gray[rowOffset + x] = luma;
                    luminanceSum += luma;
                }
            }
        });

        int pixelCount = width * height;
        double luminance = pixelCount > 0 ? luminanceSum / pixelCount : 0.0;

        double sharpness = ComputeLaplacianVariance(gray, width, height, cancellationToken);

        int longSide = Math.Max(width, height);
        var tier = ClassifyTier(sharpness, luminance, longSide);

        return Task.FromResult((tier, sharpness, luminance, width, height));
    }

    // Laplacian variance using a 4-neighbor kernel:
    //    0  1  0
    //    1 -4  1
    //    0  1  0
    // Operates on the 0-255 scale to match the threshold conventions in the spec
    // (the gray buffer is in [0,1], so each Laplacian value is multiplied back by 255).
    private static double ComputeLaplacianVariance(float[] gray, int width, int height, CancellationToken cancellationToken)
    {
        if (width < 3 || height < 3)
            return 0.0;

        double sum = 0.0;
        double sumOfSquares = 0.0;
        long count = 0;

        for (int y = 1; y < height - 1; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = y * width;
            for (int x = 1; x < width - 1; x++)
            {
                int i = rowOffset + x;
                double l = (gray[i - width] + gray[i + width] + gray[i - 1] + gray[i + 1] - 4 * gray[i]) * 255.0;
                sum += l;
                sumOfSquares += l * l;
                count++;
            }
        }

        if (count == 0)
            return 0.0;

        double mean = sum / count;
        return (sumOfSquares / count) - (mean * mean);
    }

    // CSP-189: User-facing reason for a Low-tier failure. Lighting issues take
    // priority over blur — when both are bad, the user-visible problem is almost
    // always the lighting, and "blurry" is the catch-all for low sharpness.
    public string GetLowQualityReasonMessage(double sharpness, double luminance)
    {
        if (luminance < DarkLuminanceThreshold)
            return "This photo is too dark. Try better lighting and upload again.";
        if (luminance > WashedOutLuminanceThreshold)
            return "This photo is overexposed. Try adjusting the lighting and upload again.";
        return "This photo is too blurry. Steady your camera and upload again.";
    }

    private static PhotoQualityTier ClassifyTier(double sharpness, double luminance, int longSide)
    {
        if (sharpness < BlurThreshold ||
            luminance < DarkLuminanceThreshold ||
            luminance > WashedOutLuminanceThreshold)
        {
            return PhotoQualityTier.Low;
        }

        if (sharpness >= SharpThreshold &&
            luminance >= HighTierLuminanceMin &&
            luminance <= HighTierLuminanceMax &&
            longSide >= HighTierLongSidePixels)
        {
            return PhotoQualityTier.High;
        }

        return PhotoQualityTier.Medium;
    }
}
