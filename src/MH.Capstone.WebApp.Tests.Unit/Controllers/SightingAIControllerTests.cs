using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
[ExcludeFromCodeCoverage]
public class SightingAIControllerTests
{
    private Mock<ILogger<SightingAIController>> _mockLogger = null!;
    private Mock<IAIService> _mockAiService = null!;
    private Mock<ISightingsService> _mockSightingsService = null!;
    private SightingAIController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<SightingAIController>>();
        _mockAiService = new Mock<IAIService>();
        _mockSightingsService = new Mock<ISightingsService>();

        _controller = new SightingAIController(
            _mockLogger.Object,
            _mockAiService.Object,
            _mockSightingsService.Object);
    }

    [TearDown]
    public void TearDown() => _controller?.Dispose();

    private static IFormFile BuildImageFile(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "image", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }

    [Test]
    public async Task Identify_WithValidImage_ReturnsOkWithAiResult()
    {
        // Arrange
        var image = BuildImageFile(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        _mockSightingsService.Setup(s => s.ValidateImage(It.IsAny<IFormFile>())).Returns(true);
        _mockAiService
            .Setup(a => a.IdentifyImageAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiIdentificationResult("Bald Eagle", "Adult bald eagle on a pine.", true));

        // Act
        var result = await _controller.Identify(image);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.InstanceOf<AiIdentificationResult>());
        var dto = (AiIdentificationResult)ok.Value!;
        Assert.That(dto.Species, Is.EqualTo("Bald Eagle"));
        Assert.That(dto.Identified, Is.True);
        Assert.That(dto.Description, Does.Contain("bald eagle"));
    }

    [Test]
    public async Task Identify_WhenAiCannotIdentify_ReturnsOkWithIdentifiedFalse()
    {
        // Arrange
        var image = BuildImageFile(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        _mockSightingsService.Setup(s => s.ValidateImage(It.IsAny<IFormFile>())).Returns(true);
        _mockAiService
            .Setup(a => a.IdentifyImageAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiIdentificationResult("Unknown", "no wildlife visible", false));

        // Act
        var result = await _controller.Identify(image);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>(),
            "an 'Unknown' result is still a valid 200 OK — UI distinguishes via the Identified flag");
        var dto = (AiIdentificationResult)((OkObjectResult)result).Value!;
        Assert.That(dto.Identified, Is.False);
        Assert.That(dto.Species, Is.EqualTo("Unknown"));
    }

    [Test]
    public async Task Identify_WithInvalidImage_ReturnsBadRequest()
    {
        // Arrange — ValidateImage rejects (e.g. wrong type, too big, null)
        var image = BuildImageFile(new byte[] { 0x01 });
        _mockSightingsService.Setup(s => s.ValidateImage(It.IsAny<IFormFile>())).Returns(false);

        // Act
        var result = await _controller.Identify(image);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _mockAiService.Verify(
            a => a.IdentifyImageAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "should not call AI service when input image fails ValidateImage");
    }

    [Test]
    public async Task Identify_WhenAiServiceThrows_Returns503()
    {
        // Arrange
        var image = BuildImageFile(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        _mockSightingsService.Setup(s => s.ValidateImage(It.IsAny<IFormFile>())).Returns(true);
        _mockAiService
            .Setup(a => a.IdentifyImageAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Gemini unreachable"));

        // Act
        var result = await _controller.Identify(image);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(503));
    }
}
