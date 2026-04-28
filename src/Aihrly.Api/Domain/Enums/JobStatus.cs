namespace Aihrly.Api.Domain.Enums;

/// <summary>
/// A job is either accepting applications (Open) or not (Closed).
/// When a job is Closed, we treat it as if it doesn't exist for candidates — returning 404.
/// </summary>
public enum JobStatus
{
    Open,
    Closed
}
