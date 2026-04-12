using MH.Capstone.Domain.ApiContracts.Gemini;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Options;

namespace MH.Capstone.Domain.Services.Api;

/// <summary>
/// Gemini-backed implementation of <see cref="IAIService"/>.
/// RED phase: throws NotImplementedException. GREEN phase will replace this body
/// with a real HttpClient call to the Gemini generateContent endpoint.
/// </summary>
public class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiAIService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<string> AskAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GeminiAIService.AskAsync is not implemented yet (RED phase).");
    }
}
