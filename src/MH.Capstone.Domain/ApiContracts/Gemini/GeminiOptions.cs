namespace MH.Capstone.Domain.ApiContracts.Gemini;

/// <summary>
/// Strongly-typed binding for the "Gemini" section of appsettings.
/// Loaded via IOptions&lt;GeminiOptions&gt; in Program.cs.
/// </summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(Model) &&
        !string.IsNullOrWhiteSpace(BaseUrl);
}
