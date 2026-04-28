using System;
using System.Threading;
using System.Threading.Tasks;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Tests.Acceptance.Support
{
    // Simple deterministic AI service used only during acceptance test runs.
    // Keeps the AI Companion feature deterministic and avoids external network calls.
    public class TestAIService : IAIService
    {
        public Task<string> AskAsync(string userQuestion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                return Task.FromResult("Please enter a question.");

            var q = userQuestion.ToLowerInvariant();

            // If the user asks for off-topic or potentially harmful instructions, redirect to wildlife topics.
            if (q.Contains("scrape") || q.Contains("python") || q.Contains("scraping") || q.Contains("website") || q.Contains("script"))
            {
                return Task.FromResult("I can''t help with that. Here''s a wildlife tip: always keep a safe distance and observe wildlife respectfully. (wildlife)");
            }

            // Default helpful wildlife-oriented reply that satisfies acceptance checks.
            return Task.FromResult("Here''s a wildlife tip: observe animals from a distance, note habitat and behaviour to identify species. (wildlife)");
        }

        // CSP-144: deterministic stub for AI photo recognition. Real recognition is
        // exercised in unit tests (GeminiAIServiceTests). The CSP-144 BDD scenarios
        // mock at the browser fetch boundary, so this method is rarely hit — but
        // returning a sensible default keeps the contract intact.
        public Task<AiIdentificationResult> IdentifyImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            if (imageBytes is null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes must be non-null and non-empty.", nameof(imageBytes));

            return Task.FromResult(new AiIdentificationResult(
                Species:     "Bald Eagle",
                Description: "An adult bald eagle perched on a tall tree.",
                Identified:  true));
        }
    }
}
