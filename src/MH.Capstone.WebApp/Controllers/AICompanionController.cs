using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers;

[Authorize]
[Route("AICompanion")]
public class AICompanionController : Controller
{
    private readonly IAIService _aiService;
    private readonly ILogger<AICompanionController> _logger;

    public AICompanionController(IAIService aiService, ILogger<AICompanionController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    [HttpPost]
    [Route("Ask")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromForm] string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest(new { message = "Please enter a question." });

        try
        {
            var reply = await _aiService.AskAsync(question);
            return Ok(new { reply });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini API request failed");
            return StatusCode(503, new { message = "The AI Companion is temporarily unavailable. Please try again later." });
        }
    }
}
