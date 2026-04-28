using MH.Capstone.Domain.ApiContracts.Gemini;
using MH.Capstone.Domain.Services.Api;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace MH.Capstone.Domain.Tests.Unit.Services.Api;

[TestFixture]
public class GeminiAIServiceTests
{
    private GeminiAIService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "fake-key-for-unit-tests",
            Model = "gemini-2.5-flash",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/"
        });

        _service = new GeminiAIService(new HttpClient(), options);
    }

    [Test]
    public void AskAsync_WithEmptyQuestion_ShouldThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.AskAsync(string.Empty));
    }

    [Test]
    public void AskAsync_WithWhitespaceQuestion_ShouldThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.AskAsync("   "));
    }

    [Test]
    public void AskAsync_WithNullQuestion_ShouldThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.AskAsync(null!));
    }

    [Test]
    public Task AskAsync_WithValidQuestion_ShouldCallGeminiApi()
    {
        try
        {
            // This test uses a fake API key so the Gemini call will fail with 400/401.
            // The point is to prove the code reaches the HTTP call rather than throwing
            // NotImplementedException (which was the RED state).
            // A real integration test with a live key belongs in Tests.Integration.
            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await _service.AskAsync("What is a black bear?"));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    // ── CSP-144: AI Photo Recognition ─────────────────────────────────────────

    [Test]
    public void IdentifyImageAsync_WithNullBytes_ShouldThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.IdentifyImageAsync(null!));
    }

    [Test]
    public void IdentifyImageAsync_WithEmptyBytes_ShouldThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.IdentifyImageAsync(Array.Empty<byte>()));
    }

    [Test]
    public Task IdentifyImageAsync_WithValidBytes_ShouldCallGeminiApi()
    {
        // Same low-fidelity pattern as AskAsync_WithValidQuestion: a fake API key
        // causes the Gemini Vision endpoint to return 400/401, which surfaces as an
        // HttpRequestException. Proves the code reached the HTTP call path.
        try
        {
            var fakeImage = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await _service.IdentifyImageAsync(fakeImage));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
