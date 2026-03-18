using System.Diagnostics;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;

namespace MH.Capstone.WebApp.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly FeatureFlags _featureFlags;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<EmailQueue, ApplicationDbContext> _emailQueueRepo;

        public HomeController(ILogger<HomeController> logger, IEmailService emailService, 
            FeatureFlags featureFlags, UserManager<ApplicationUser> userManager, 
            INotificationService notificationService,
            IRepository<EmailQueue, ApplicationDbContext> emailQueueRepo)
        {
            _logger = logger;
            _emailService = emailService;
            _featureFlags = featureFlags;
            _userManager = userManager;
            _notificationService = notificationService;
            _emailQueueRepo = emailQueueRepo;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [Route("privacy")]
        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("about")]
        public IActionResult About()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("error")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize]
        [HttpGet]
        [Route("uat/emailer")]
        [Route("uat/email")]
        public IActionResult TestEmailView()
        {
            // Check feature flag first
            if (!_featureFlags.IsEnabled("EnableEmailTestEndpoint"))
            {
                // Feature disabled - do nothing and return 404 so endpoint is not discoverable
                _logger.LogInformation("Email test endpoint called but feature flag is disabled.");
                return NotFound();
            }

            return View("EmailSenderUAT");
        }

        /// <summary>
        /// Test endpoint to enqueue a simple email to the currently authenticated user's email address.
        /// Route: /uat/emailer
        /// Only active when FeatureFlag "EnableEmailTestEndpoint" is enabled.
        /// </summary>
        [Authorize]
        [HttpPost]
        [Route("uat/emailer")]
        [Route("uat/email")]
        public async Task<IActionResult> SendTestEmail()
        {
            // Check feature flag first
            if (!_featureFlags.IsEnabled("EnableEmailTestEndpoint"))
            {
                // Feature disabled - do nothing and return 404 so endpoint is not discoverable
                _logger.LogInformation("Email test endpoint called but feature flag is disabled.");
                return NotFound();
            }

            // Get the currently signed-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var recipientEmail = user.Email;
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return BadRequest("Signed-in user does not have an email address configured.");
            }

            const string subject = "WildlifeAID - Test Email";
            var body = $"This is a test email from the WildlifeAID Team at {DateTimeOffset.UtcNow}!";

            try
            {
                var queued = new EmailQueue
                {
                    Recipient = recipientEmail,
                    Subject = subject,
                    HtmlBody = body,
                    PlainTextBody = null,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ScheduledAt = null
                };

                await queued.EnqueueAsync(_emailQueueRepo).ConfigureAwait(false);

                _logger.LogInformation("Test email enqueued for {Email} (EmailQueueId={Id})", recipientEmail, queued.Id);

                await _notificationService.SendNotificationAsync(Notification
                    .Create(user.GuidId, "Test Email Enqueued!",
                        $"A test email has been queued to your email {user.Email} " +
                        $"from our email {_emailService.SenderAddress} and will be sent soon!"));

                return RedirectToAction("Notifications", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue test email to {Email}", recipientEmail);
                return StatusCode(500, "Failed to enqueue test email.");
            }
        }
    }
}
