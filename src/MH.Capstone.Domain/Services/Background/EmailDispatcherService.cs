using MH.Capstone.Domain.Constants.Configurables;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MH.Capstone.Domain.Services.Background
{
    public class EmailDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailDispatcherService> _logger;
        private readonly EmailDispatcherOptions _options;

        public EmailDispatcherService(IServiceScopeFactory scopeFactory, ILogger<EmailDispatcherService> logger, 
            IOptions<EmailDispatcherOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options?.Value ?? new EmailDispatcherOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailDispatcherService started. IntervalSeconds={IntervalSeconds} BatchSize={BatchSize}", _options.IntervalSeconds, _options.BatchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error while dispatching emails");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken).ConfigureAwait(false);
            }

            _logger.LogInformation("EmailDispatcherService stopping.");
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<EmailQueue, ApplicationDbContext>>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Select pending emails not already being processed and scheduled for now
            var now = DateTimeOffset.UtcNow;
            var allQueryable = await repo.GetAllAsync().ConfigureAwait(false);

            var items = await allQueryable
                .Where(e => !e.IsSent && !e.Processing && (e.ScheduledAt == null || e.ScheduledAt <= now) && e.Attempts < _options.MaxAttempts)
                .OrderBy(e => e.CreatedAt)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (items.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Dispatching {Count} pending emails", items.Count);

            // Mark them as Processing to reduce likelihood of duplicate processing across instances
            foreach (var it in items)
            {
                it.Processing = true;
                it.LastAttemptAt = now;
                it.Attempts++;
                try
                {
                    await repo.AddOrUpdateAsync(it).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to mark email {Id} as processing", it.Id);
                }
            }

            foreach (var email in items)
            {
                try
                {
                    await emailService.SendAsync(email.Recipient, email.Subject, email.HtmlBody, email.PlainTextBody, cancellationToken).ConfigureAwait(false);

                    email.IsSent = true;
                    email.SentAt = DateTimeOffset.UtcNow;
                    email.Processing = false;
                    email.LastError = null;
                    _logger.LogInformation("Email sent to {Recipient} (EmailQueueId={Id})", email.Recipient, email.Id);

                    try
                    {
                        await repo.AddOrUpdateAsync(email).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist sent status for email {Id}", email.Id);
                    }
                }
                catch (Exception ex)
                {
                    email.Processing = false;
                    email.LastError = ex.Message;
                    _logger.LogWarning(ex, "Failed to send queued email {Id} to {Recipient}. Attempts={Attempts}", email.Id, email.Recipient, email.Attempts);

                    // schedule next attempt with exponential backoff
                    var backoff = TimeSpan.FromSeconds(_options.RetryBackoffSeconds * Math.Pow(2, Math.Max(0, email.Attempts - 1)));
                    email.ScheduledAt = DateTimeOffset.UtcNow.Add(backoff);

                    try
                    {
                        await repo.AddOrUpdateAsync(email).ConfigureAwait(false);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "Failed to persist retry schedule for email {Id}", email.Id);
                    }
                }
            }
        }
    }
}
