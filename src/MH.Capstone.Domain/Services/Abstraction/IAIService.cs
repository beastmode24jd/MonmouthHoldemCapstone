namespace MH.Capstone.Domain.Services.Abstraction;

/// <summary>
/// AI service for the WildlifeAID app, currently backed by Gemini.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Ask the AI Companion (CSP-120) a question and get a plain-text reply.
    /// The system prompt is applied server-side and never exposed to the caller.
    /// </summary>
    Task<string> AskAsync(string userQuestion, CancellationToken cancellationToken = default);

    /// <summary>
    /// CSP-144: identify the wildlife species in the given photo using Gemini Vision.
    /// Returns the common species name and a short description, plus an Identified flag
    /// that is false when the AI cannot confidently match a species (e.g. non-wildlife
    /// photo, ambiguous shot). Callers must already have validated the image (CSP-122
    /// photo quality gate) before invoking — this method does not re-check size/type.
    /// </summary>
    /// <param name="imageBytes">Raw image bytes (JPEG/PNG, &lt;= 2 MB, long-side &gt;= 1024 px).</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    Task<AiIdentificationResult> IdentifyImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
}
