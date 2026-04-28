namespace Aihrly.Api.Domain.Enums;

/// <summary>
/// The types of notifications dispatched by the background job.
/// A notification is created when an application reaches a terminal stage.
/// </summary>
public enum NotificationType
{
    Hired,
    Rejected
}
