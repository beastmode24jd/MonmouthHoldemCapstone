using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Services
{
    public class NoOpEmailService : IEmailService, IDisposable
    {
        private readonly string _senderAddress;
        private readonly ILogger<NoOpEmailService> _logger;
        private bool _disposed;

        public NoOpEmailService(string senderAddress, ILogger<NoOpEmailService> logger)
        {
            _senderAddress = string.IsNullOrWhiteSpace(senderAddress) ? "no-reply@localhost" : senderAddress;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("NoOpEmailService initialized with sender {Sender}", _senderAddress);
        }

        public Task SendAsync(string to, string subject, string htmlBody, string? plainText = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("Recipient is required", nameof(to));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required", nameof(subject));

            // Do not send anything. Just log what we would have done.
            _logger.LogInformation("[NoOpEmailService] Pretend send from {Sender} to {Recipient} with Subject '{Subject}'. HtmlLength={HtmlLen} PlainTextLength={PlainLen}",
                _senderAddress, to, subject, htmlBody?.Length ?? 0, plainText?.Length ?? 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
