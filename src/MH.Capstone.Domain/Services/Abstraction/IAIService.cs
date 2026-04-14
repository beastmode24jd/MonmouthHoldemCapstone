namespace MH.Capstone.Domain.Services.Abstraction;

/// <summary>
/// AI Companion service for the WildlifeAID AI Companion feature (CSP-120).
/// Sends a user question to an AI backend with a hidden system prompt that
/// constrains the assistant to wildlife-education and observer-safety topics.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Ask the AI Companion a question and get a plain-text reply.
    /// The system prompt is applied server-side and never exposed to the caller.
    /// </summary>
    /// <param name="userQuestion">The end-user's question text.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>Plain-text reply from the AI backend.</returns>
    Task<string> AskAsync(string userQuestion, CancellationToken cancellationToken = default);
}
