using Xunit;
using FluentAssertions;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Notifications;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aihrly.Tests.Features.NotificationTests;

public class NotificationQueueTests
{
    // -----------------------------------------------------------------------
    // NotificationQueue
    // -----------------------------------------------------------------------

    [Fact]
    public void Enqueue_ThenDequeue_ShouldReturnSameJob()
    {
        var queue = new NotificationQueue();
        var job = new NotificationJob(Guid.NewGuid(), NotificationType.Hired);

        queue.TryWrite(job);

        var success = queue.TryRead(out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.ApplicationId.Should().Be(job.ApplicationId);
        result.Type.Should().Be(NotificationType.Hired);
    }

    [Fact]
    public void TryDequeue_OnEmptyQueue_ShouldReturnFalse()
    {
        var queue = new NotificationQueue();

        var success = queue.TryRead(out var result);

        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Enqueue_MultipleJobs_ShouldDequeueInOrder()
    {
        // Channel is FIFO: first in, first out.
        var queue = new NotificationQueue();

        var job1 = new NotificationJob(Guid.NewGuid(), NotificationType.Hired);
        var job2 = new NotificationJob(Guid.NewGuid(), NotificationType.Rejected);

        queue.TryWrite(job1);
        queue.TryWrite(job2);

        queue.TryRead(out var first);
        queue.TryRead(out var second);

        first!.Type.Should().Be(NotificationType.Hired);
        second!.Type.Should().Be(NotificationType.Rejected);
    }

    // -----------------------------------------------------------------------
    // NotificationService
    // -----------------------------------------------------------------------

    [Fact]
    public void EnqueueNotification_ShouldAddJobToQueue()
    {
        var queue = new NotificationQueue();
        var service = new NotificationService(
            queue,
            NullLogger<NotificationService>.Instance);
        var appId = Guid.NewGuid();

        // EnqueueNotification must return immediately: no await.
        service.EnqueueNotification(appId, NotificationType.Rejected);

        // Verify the job landed in the queue.
        var success = queue.TryRead(out var job);

        success.Should().BeTrue();
        job!.ApplicationId.Should().Be(appId);
        job.Type.Should().Be(NotificationType.Rejected);
    }

    [Fact]
    public void EnqueueNotification_ShouldNotBlock_CallingThread()
    {
        // EnqueueNotification is void, so it must never await anything.
        // This test verifies it completes in under 100ms.
        var queue = new NotificationQueue();
        var service = new NotificationService(
            queue,
            NullLogger<NotificationService>.Instance);

        var start = DateTime.UtcNow;
        service.EnqueueNotification(Guid.NewGuid(), NotificationType.Hired);
        var elapsed = DateTime.UtcNow - start;

        elapsed.TotalMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task BackgroundProcessor_ShouldDrainQueue_AndPersistNotification()
    {
        var queue = new NotificationQueue();
        var databaseName = Guid.NewGuid().ToString();

        var services = new ServiceCollection()
            .AddDbContext<AihrlyDbContext>(options =>
                options.UseInMemoryDatabase(databaseName))
            .BuildServiceProvider();

        await SeedApplicationAsync(services, applicationId: TestApplicationId);

        var processor = new BackgroundNotificationProcessor(
            queue,
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BackgroundNotificationProcessor>.Instance);

        await processor.StartAsync(CancellationToken.None);

        try
        {
            queue.TryWrite(new NotificationJob(TestApplicationId, NotificationType.Hired));

            var notification = await WaitForNotificationAsync(services, TestApplicationId);

            notification.Should().NotBeNull();
            notification!.ApplicationId.Should().Be(TestApplicationId);
            notification.Type.Should().Be(NotificationType.Hired);
            notification.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
            await services.DisposeAsync();
        }
    }

    private static readonly Guid TestApplicationId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static async Task SeedApplicationAsync(
        ServiceProvider services,
        Guid applicationId)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AihrlyDbContext>();

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Software Engineer",
            Description = "Build APIs",
            Location = "Accra",
            Status = JobStatus.Open
        };

        db.Jobs.Add(job);
        db.Applications.Add(new Application
        {
            Id = applicationId,
            JobId = job.Id,
            CandidateName = "Ama Mensah",
            CandidateEmail = "ama@example.com",
            Stage = ApplicationStage.Offer,
            AppliedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task<Notification?> WaitForNotificationAsync(
        ServiceProvider services,
        Guid applicationId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(3);

        while (DateTime.UtcNow < deadline)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AihrlyDbContext>();
            var notification = await db.Notifications
                .SingleOrDefaultAsync(n => n.ApplicationId == applicationId);

            if (notification is not null)
                return notification;

            await Task.Delay(25);
        }

        return null;
    }
}
