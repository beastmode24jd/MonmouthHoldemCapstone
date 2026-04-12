using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MH.Capstone.Domain.ApiContracts.Gemini;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Options;

namespace MH.Capstone.Domain.Services.Api;

public class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    private const string SystemPrompt =
        "You are a friendly wildlife education companion for the WildlifeAID app. " +
        "Your purpose is to help users learn about wildlife identification, animal behavior, " +
        "habitat conservation, and observer safety. " +
        "If a user asks something unrelated to wildlife, nature, or outdoor safety, " +
        "politely decline and steer the conversation back to wildlife topics. " +
        "Keep answers concise (under 200 words) and suitable for a general audience.";

    public GeminiAIService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> AskAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuestion))
            throw new ArgumentException("Question cannot be empty.", nameof(userQuestion));

        var url = $"{_options.BaseUrl.TrimEnd('/')}/" +
                  $"models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = SystemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = userQuestion } }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var reply = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return reply ?? string.Empty;
    }
}
