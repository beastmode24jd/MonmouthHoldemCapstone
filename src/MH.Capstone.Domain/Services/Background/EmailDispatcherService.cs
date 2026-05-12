using MH.Capstone.Domain.Constants.Configurables;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace MH.Capstone.Domain.Services.Background
{
    public class EmailDispatcherService : BackgroundService
    {
        private readonly ChannelReader<EmailMessage> _channelReader;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailDispatcherService> _logger;
        private readonly EmailDispatcherOptions _options;

        public EmailDispatcherService(
            ChannelReader<EmailMessage> channelReader,
            IEmailService emailService,
            ILogger<EmailDispatcherService> logger,
            IOptions<EmailDispatcherOptions> options)
        {
            _channelReader = channelReader;
            _emailService = emailService;
            _logger = logger;
            _options = options?.Value ?? new EmailDispatcherOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailDispatcherService started.");

            await foreach (var message in _channelReader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                _ = SendWithRetryAsync(message, attempt: 0, stoppingToken);
            }

            _logger.LogInformation("EmailDispatcherService stopping.");
        }

        private async Task SendWithRetryAsync(EmailMessage message, int attempt, CancellationToken ct)
        {
            try
            {
                await _emailService.SendAsync(message.Recipient, message.Subject, message.HtmlBody, message.PlainTextBody, ct).ConfigureAwait(false);
                _logger.LogInformation("Email sent to {Recipient}", message.Recipient);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex) when (attempt < _options.MaxAttempts - 1)
            {
                _logger.LogWarning(ex, "Failed to send email to {Recipient}. Attempt={Attempt}, retrying", message.Recipient, attempt + 1);
                var backoff = TimeSpan.FromSeconds(_options.RetryBackoffSeconds * Math.Pow(2, attempt));
                await Task.Delay(backoff, ct).ConfigureAwait(false);
                await SendWithRetryAsync(message, attempt + 1, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permanently failed to send email to {Recipient} after {Attempts} attempt(s)", message.Recipient, attempt + 1);
            }
        }
    }
}
