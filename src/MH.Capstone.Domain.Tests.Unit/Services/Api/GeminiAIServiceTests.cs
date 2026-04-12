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
        var httpClient = new HttpClient();
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "fake-key",
            Model = "gemini-2.5-flash",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/"
        });

        _service = new GeminiAIService(httpClient, options);
    }

    [Test]
    public async Task AskAsync_WithValidQuestion_ShouldReturnNonEmptyReply()
    {
        // RED: GeminiAIService throws NotImplementedException. This test will go
        // GREEN once the real Gemini HTTP call is wired up.
        var reply = await _service.AskAsync("What should I know about black bear safety?");

        Assert.That(reply, Is.Not.Null);
        Assert.That(reply, Is.Not.Empty);
    }

    [Test]
    public void AskAsync_WithEmptyQuestion_ShouldThrowArgumentException()
    {
        // RED: Once GREEN lands, empty input should be rejected at the service boundary.
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.AskAsync(string.Empty));
    }
}
