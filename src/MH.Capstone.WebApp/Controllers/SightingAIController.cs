using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    /// <summary>
    /// CSP-144: AI-assisted species identification for the sighting upload flow.
    /// Accepts an uploaded image and returns the AI's best guess at the species
    /// plus a short description, which the upload form pre-fills.
    /// </summary>
    [Authorize]
    [Route("SightingAI")]
    public class SightingAIController : Controller
    {
        private readonly ILogger<SightingAIController> _logger;
        private readonly IAIService _aiService;
        private readonly ISightingsService _sightingsService;

        public SightingAIController(ILogger<SightingAIController> logger,
            IAIService aiService, ISightingsService sightingsService)
        {
            _logger = logger;
            _aiService = aiService;
            _sightingsService = sightingsService;
        }

        /// <summary>
        /// POST /SightingAI/Identify — multipart/form-data with a single image field.
        /// Returns 200 + AiIdentificationResult JSON (Identified=false when the AI
        /// can't confidently match a species), 400 if the image fails validation,
        /// or 503 if the AI service is unreachable.
        /// </summary>
        [HttpPost]
        [Route("Identify")]
        public async Task<IActionResult> Identify(IFormFile? image)
        {
            // Reuse CSP-122/CSP-53's existing image validation (file type + size).
            // This is the same gate the sighting upload form already uses.
            if (!_sightingsService.ValidateImage(image))
            {
                return BadRequest(new
                {
                    message = "Image must be a valid JPG or PNG file under 2 MB."
                });
            }

            try
            {
                var bytes = image!.ToByteArray();
                var result = await _aiService.IdentifyImageAsync(bytes);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI identification failed for sighting upload.");
                return StatusCode(503, new
                {
                    message = "Could not reach the AI service. Please try again, or describe the photo manually."
                });
            }
        }
    }
}
