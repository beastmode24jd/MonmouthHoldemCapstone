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

    // CSP-144: prompt for the photo-recognition flow. Strict format makes the
    // response trivially parseable; the "Unknown" fallback handles non-wildlife photos
    // without requiring confidence scores from the model.
    private const string IdentifyPrompt =
        "You are a wildlife identification expert. Identify the species in this photo. " +
        "Respond in this EXACT format on two lines, no markdown, no extra commentary:\n" +
        "SPECIES: <common name of the species>\n" +
        "DESCRIPTION: <one or two sentences describing what is visible in the photo>\n\n" +
        "If the photo does not clearly show a wild animal, or you cannot confidently " +
        "identify the species, respond:\n" +
        "SPECIES: Unknown\n" +
        "DESCRIPTION: <brief reason, e.g. 'no wildlife visible' or 'image too ambiguous'>";

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

    public async Task<AiIdentificationResult> IdentifyImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes must be non-null and non-empty.", nameof(imageBytes));

        var url = $"{_options.BaseUrl.TrimEnd('/')}/" +
                  $"models/{_options.Model}:generateContent?key={_options.ApiKey}";

        // Gemini Vision accepts inline base64 image data alongside text in a single content part.
        // MIME type "image/jpeg" works for both JPEG and PNG payloads in practice.
        var base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = IdentifyPrompt },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                    }
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

        var rawText = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return ParseIdentificationResponse(rawText);
    }

    // Parses the strict "SPECIES: X\nDESCRIPTION: Y" format defined in IdentifyPrompt.
    // Tolerant of casing, surrounding whitespace, and trailing markdown the model may sneak in.
    internal static AiIdentificationResult ParseIdentificationResponse(string rawText)
    {
        string species = string.Empty;
        string description = string.Empty;

        foreach (var rawLine in rawText.Split('\n'))
        {
            var line = rawLine.Trim().Trim('*', '_', '`');

            if (line.StartsWith("SPECIES:", StringComparison.OrdinalIgnoreCase))
                species = line["SPECIES:".Length..].Trim();
            else if (line.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                description = line["DESCRIPTION:".Length..].Trim();
        }

        var identified = !string.IsNullOrWhiteSpace(species)
                         && !species.Equals("Unknown", StringComparison.OrdinalIgnoreCase);

        return new AiIdentificationResult(
            Species:     identified ? species : "Unknown",
            Description: description,
            Identified:  identified);
    }
}
