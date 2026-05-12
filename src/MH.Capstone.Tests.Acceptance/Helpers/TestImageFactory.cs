using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MH.Capstone.Tests.Acceptance.Helpers;

/// <summary>
/// Generates synthetic image files on disk for use in acceptance tests.
/// </summary>
/// <remarks>
/// All methods write a PNG to the system temp directory and return its absolute path.
/// Callers are responsible for deleting the file when the test finishes (e.g. in
/// an [AfterScenario] hook). Use <see cref="CreateByQuality"/> when a specific
/// CSP-122 quality tier is required; use <see cref="CreateValid"/> when any
/// passing image will do.
///
/// Every generated image is ≥1024 px on the long side so it clears the hard
/// resolution gate in SightingController. Images are intentionally minimal — no
/// EXIF, no colour profiles — so they compress to a few KB.
/// </remarks>
public static class TestImageFactory
{
    // Named presets that deterministically map to a CSP-122 PhotoQualityTier outcome.
    // Mirrors the thresholds in PhotoQualityService:
    //   Sharpness < 100   → Low (blurry)
    //   Luminance < 0.20  → Low (dark)
    //   Luminance > 0.85  → Low (washed-out)
    //   Sharpness ≥ 300 && 0.30 ≤ Luminance ≤ 0.75 && longSide ≥ 2048 → High
    //   Anything else that passes → Medium
    private static readonly IReadOnlyDictionary<string, Func<Image<Rgba32>>> Presets =
        new Dictionary<string, Func<Image<Rgba32>>>(StringComparer.OrdinalIgnoreCase)
        {
            // Solid mid-gray: sharpness ≈ 0 (< 100) → Low (blurry path)
            ["blurry"]       = () => new Image<Rgba32>(1280, 960,  new Rgba32(128, 128, 128, 255)),
            // Solid near-black: luminance ≈ 0.04 (< 0.20) → Low (dark warning)
            ["low-light"]    = () => new Image<Rgba32>(1280, 960,  new Rgba32(10,  10,  10,  255)),
            // Solid near-white: luminance ≈ 0.94 (> 0.85) → Low (washed-out warning)
            ["overexposed"]  = () => new Image<Rgba32>(1280, 960,  new Rgba32(240, 240, 240, 255)),
            // Vertical stripes at 2400×1800: high Laplacian variance, luminance ≈ 0.5 → High
            ["high-quality"] = () => CreateVerticalStripesImage(2400, 1800),
        };

    /// <summary>
    /// Creates a synthetic PNG whose pixels map deterministically to the given quality label.
    /// </summary>
    /// <param name="quality">One of: blurry, low-light, overexposed, high-quality (case-insensitive).</param>
    /// <returns>Absolute path to the generated temp file.</returns>
    public static string CreateByQuality(string quality)
    {
        if (!Presets.TryGetValue(quality, out var factory))
            throw new ArgumentException(
                $"Unknown quality preset '{quality}'. Expected: {string.Join(", ", Presets.Keys)}.",
                nameof(quality));

        var path = Path.Combine(Path.GetTempPath(), $"test_image_{quality}_{Guid.NewGuid():N}.png");
        using var image = factory();
        image.SaveAsPng(path);
        return path;
    }

    /// <summary>
    /// Creates a generic valid PNG that passes all photo quality gates (resolution ≥1024 px,
    /// decodable by ImageSharp). Use this when a test only needs a sighting upload to succeed
    /// and does not care about the resulting quality tier.
    /// </summary>
    /// <remarks>
    /// CSP-189: must be a non-Low-tier image, since the upload form now rejects Low-tier
    /// photos rather than accepting-with-warning. Uses the "high-quality" stripes preset.
    /// </remarks>
    /// <returns>Absolute path to the generated temp file.</returns>
    public static string CreateValid()
        => CreateByQuality("high-quality");

    private static Image<Rgba32> CreateVerticalStripesImage(int width, int height)
    {
        var black = new Rgba32(0,   0,   0,   255);
        var white = new Rgba32(255, 255, 255, 255);
        var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                    row[x] = (x % 2 == 0) ? black : white;
            }
        });
        return image;
    }
}
