using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Infrastructure.Persistence;
using System.Threading.Channels;

namespace Aihrly.Api.Features.Notifications;

/// <summary>
/// A thread-safe channel that carries notification jobs from the HTTP
/// request thread (producer) to the background processor (consumer).
/// Registered as Singleton — both producer and consumer share the
/// exact same instance guaranteed.
/// </summary>
public class NotificationQueue
{
    // Channel is the modern .NET alternative to ConcurrentQueue for
    // producer/consumer patterns. It is thread-safe and supports async.
    // Capacity 100 means up to 100 unprocessed notifications can queue up.
    private readonly Channel<NotificationJob> _channel =
        Channel.CreateBounded<NotificationJob>(100);

    /// <summary>
    /// Called by NotificationService — never blocks the HTTP thread.
    /// TryWrite returns false if the channel is full (very unlikely).
    /// </summary>
    public bool TryWrite(NotificationJob job) =>
        _channel.Writer.TryWrite(job);

    /// <summary>
    /// Called by BackgroundNotificationProcessor — waits asynchronously
    /// for the next job. Returns false when the channel is closed.
    /// </summary>
    public ValueTask<bool> WaitToReadAsync(CancellationToken ct) =>
        _channel.Reader.WaitToReadAsync(ct);

    public bool TryRead(out NotificationJob? job) =>
        _channel.Reader.TryRead(out job);
}

public record NotificationJob(Guid ApplicationId, NotificationType Type);

/// <summary>
/// Called by ApplicationService immediately after a stage moves to
/// Hired or Rejected. EnqueueNotification writes to the channel and
/// returns instantly — the HTTP response is never delayed.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly NotificationQueue _queue;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(NotificationQueue queue,
        ILogger<NotificationService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public void EnqueueNotification(Guid applicationId, NotificationType type)
    {
        var job = new NotificationJob(applicationId, type);
        var success = _queue.TryWrite(job);

        if (success)
            _logger.LogInformation(
                "[NotificationQueue] Job enqueued — ApplicationId: {ApplicationId}, Type: {Type}",
                applicationId, type);
        else
            _logger.LogWarning(
                "[NotificationQueue] Channel full — notification dropped for ApplicationId: {ApplicationId}",
                applicationId);
    }
}

/// <summary>
/// Long-running background service that drains the notification channel.
/// Uses async waiting (WaitToReadAsync) instead of polling with Task.Delay
/// — this means it reacts instantly when a job arrives rather than
/// waiting up to 500ms.
/// </summary>
public class BackgroundNotificationProcessor : BackgroundService
{
    private readonly NotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundNotificationProcessor> _logger;

    public BackgroundNotificationProcessor(
        NotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundNotificationProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[BackgroundProcessor] Notification processor started and waiting for jobs.");

        try
        {
            // WaitToReadAsync suspends the thread cheaply until a job arrives.
            // When a job is written to the channel this resumes immediately.
            while (await _queue.WaitToReadAsync(stoppingToken))
            {
                while (_queue.TryRead(out var job) && job is not null)
                {
                    _logger.LogInformation(
                        "[BackgroundProcessor] Job received — ApplicationId: {ApplicationId}, Type: {Type}",
                        job.ApplicationId, job.Type);

                    await ProcessAsync(job, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — app is stopping
            _logger.LogInformation(
                "[BackgroundProcessor] Notification processor stopped.");
        }
    }

    private async Task ProcessAsync(NotificationJob job, CancellationToken ct)
    {
        try
        {
            // Create a fresh DI scope so we get a new DbContext.
            // BackgroundService is Singleton — DbContext is Scoped.
            // We must never inject DbContext directly into a Singleton.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<AihrlyDbContext>();

            _logger.LogInformation(
                "[BackgroundProcessor] Saving notification — ApplicationId: {ApplicationId}, Type: {Type}",
                job.ApplicationId, job.Type);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                ApplicationId = job.ApplicationId,
                Type = job.Type,
                SentAt = DateTime.UtcNow
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[BackgroundProcessor] Notification saved — Id: {NotificationId}",
                notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[BackgroundProcessor] Failed to process notification for ApplicationId: {ApplicationId}",
                job.ApplicationId);
        }
    }
}
