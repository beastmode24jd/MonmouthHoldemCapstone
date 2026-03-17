using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class AzureCommunicationEmailService : IEmailService, IDisposable
    {
        private readonly EmailClient _client;
        private readonly string _senderAddress;
        private readonly ILogger<AzureCommunicationEmailService> _logger;
        private bool _disposed;

        public AzureCommunicationEmailService(string connectionString, string senderAddress, ILogger<AzureCommunicationEmailService> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Azure Communication Services connection string is required.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                throw new ArgumentException("Sender address is required.", nameof(senderAddress));
            }

            _client = new EmailClient(connectionString);
            _senderAddress = senderAddress;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendAsync(string to, string subject, string htmlBody, string? plainText = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("Recipient is required", nameof(to));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required", nameof(subject));

            var content = new EmailContent(subject)
            {
                Html = htmlBody ?? string.Empty,
                PlainText = plainText ?? string.Empty
            };

            var recipient = new EmailAddress(to);
            var recipients = new EmailRecipients([recipient]);

            var message = new EmailMessage(_senderAddress, recipients, content);

            try
            {
                var response = await _client.SendAsync(WaitUntil.Started, message, cancellationToken);
                _logger.LogInformation("Email queued. Status: {MessageId}", response.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", message);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            // EmailClient does not implement IDisposable (as of SDK design), but if it does in the future, dispose here.
            _disposed = true;
        }
    }
}
