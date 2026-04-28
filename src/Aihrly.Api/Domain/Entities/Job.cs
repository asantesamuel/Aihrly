using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain.Entities;

/// <summary>
/// Represents a job posting that candidates can apply to.
/// A closed job returns 404 when a candidate tries to apply — it is treated
/// as if it doesn't exist from the candidate's perspective.
/// </summary>
public class Job
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Open;

    // Navigation: a job can have many applications
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
