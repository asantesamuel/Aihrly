using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain.Entities;

/// <summary>
/// A record of a notification dispatched by the background job.
/// When an application moves to Hired or Rejected, the PATCH /stage endpoint
/// returns immediately, and a BackgroundService asynchronously inserts a row here
/// and writes a log line — simulating sending an email.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    // Foreign key to Application
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public NotificationType Type { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
