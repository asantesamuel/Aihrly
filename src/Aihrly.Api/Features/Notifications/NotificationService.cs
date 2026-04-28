using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Infrastructure.Persistence;
using System.Collections.Concurrent;

namespace Aihrly.Api.Features.Notifications;

/// <summary>
/// Queues notification jobs using an in-memory channel.
/// EnqueueNotification returns immediately — the HTTP response is not blocked.
/// The BackgroundNotificationProcessor (a BackgroundService) drains the queue.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly NotificationQueue _queue;

    public NotificationService(NotificationQueue queue)
    {
        _queue = queue;
    }

    public void EnqueueNotification(Guid applicationId, NotificationType type)
    {
        _queue.Enqueue(new NotificationJob(applicationId, type));
    }
}

/// <summary>
/// A simple thread-safe in-memory queue shared between the
/// NotificationService (producer) and BackgroundNotificationProcessor (consumer).
/// </summary>
public class NotificationQueue
{
    private readonly ConcurrentQueue<NotificationJob> _jobs = new();

    public void Enqueue(NotificationJob job) => _jobs.Enqueue(job);
    public bool TryDequeue(out NotificationJob? job) => _jobs.TryDequeue(out job);
}

public record NotificationJob(Guid ApplicationId, NotificationType Type);

/// <summary>
/// Long-running background service that processes notification jobs.
/// Registered as a hosted service in Program.cs.
/// On each tick it drains the queue, writes a log line, and inserts
/// a row into the Notifications table — simulating sending an email.
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
        // TODO: implement queue draining logic
        // Will use _scopeFactory to create a DbContext scope per job
        // (BackgroundService is singleton; DbContext is scoped)
        await Task.CompletedTask;
    }
}
