using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Features.Notifications;

/// <summary>
/// Defines the contract for dispatching notifications.
/// The concrete implementation queues a background job — the caller
/// (the stage service) never waits for the notification to complete.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Enqueues a notification to be sent asynchronously.
    /// Called after an application reaches Hired or Rejected.
    /// Returns immediately — does not block the HTTP response.
    /// </summary>
    void EnqueueNotification(Guid applicationId, NotificationType type);
}
