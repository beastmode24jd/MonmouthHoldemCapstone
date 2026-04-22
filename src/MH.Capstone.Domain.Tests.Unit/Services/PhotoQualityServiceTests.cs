using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Tests.Unit.Services;

// CSP-122 Photo Quality Gate — tests for PhotoQualityService.AnalyzeAsync
[TestFixture]
[Parallelizable]
[ExcludeFromCodeCoverage]
public class PhotoQualityServiceTests
{
    // Remember: Arrange, Act, Assert

    private PhotoQualityService CreateSut() => new PhotoQualityService();

    // Produce a valid JPEG of the requested dimensions for tests that need real image bytes.
    private static byte[] CreateTestImageBytes(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    // Produce a valid PNG filled with a single solid color. Used for luminance tests where
    // we need every pixel to have a known, predictable value. PNG is lossless so the color
    // values round-trip exactly (unlike JPEG, which would shift them slightly).
    private static byte[] CreateSolidImageBytes(int width, int height, Rgba32 fillColor)
    {
        using var image = new Image<Rgba32>(width, height, fillColor);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    [Test]
    public void AnalyzeAsync_WithNullBytes_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        byte[] imageBytes = null!;

        // Act + Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.AnalyzeAsync(imageBytes));
    }

    [Test]
    public async Task AnalyzeAsync_WithValidImage_ReturnsCorrectDimensions()
    {
        // Arrange
        var sut = CreateSut();
        var imageBytes = CreateTestImageBytes(1600, 1200);

        // Act
        var result = await sut.AnalyzeAsync(imageBytes);

        // Assert
        Assert.That(result.Width, Is.EqualTo(1600));
        Assert.That(result.Height, Is.EqualTo(1200));
    }

    [Test]
    public async Task AnalyzeAsync_WithDarkImage_ReturnsLowLuminanceAndLowTier()
    {
        // Arrange — solid near-black image (pixel luminance ≈ 0.04)
        var sut = CreateSut();
        var imageBytes = CreateSolidImageBytes(1600, 1200, new Rgba32(10, 10, 10, 255));

        // Act
        var result = await sut.AnalyzeAsync(imageBytes);

        // Assert
        Assert.That(result.Luminance, Is.LessThan(0.20));
        Assert.That(result.Tier, Is.EqualTo(PhotoQualityTier.Low));
    }

    [Test]
    public async Task AnalyzeAsync_WithWashedOutImage_ReturnsHighLuminanceAndLowTier()
    {
        // Arrange — solid near-white image (pixel luminance ≈ 0.94)
        var sut = CreateSut();
        var imageBytes = CreateSolidImageBytes(1600, 1200, new Rgba32(240, 240, 240, 255));

        // Act
        var result = await sut.AnalyzeAsync(imageBytes);

        // Assert
        Assert.That(result.Luminance, Is.GreaterThan(0.85));
        Assert.That(result.Tier, Is.EqualTo(PhotoQualityTier.Low));
    }

    [Test]
    public async Task AnalyzeAsync_WithMidLuminanceImage_ReturnsLuminanceInValidRange()
    {
        // Arrange — solid middle-gray image (pixel luminance ≈ 0.50)
        var sut = CreateSut();
        var imageBytes = CreateSolidImageBytes(1600, 1200, new Rgba32(128, 128, 128, 255));

        // Act
        var result = await sut.AnalyzeAsync(imageBytes);

        // Assert — luminance should be neither too low nor too high; tier logic comes later.
        Assert.That(result.Luminance, Is.InRange(0.20, 0.85));
    }
}
