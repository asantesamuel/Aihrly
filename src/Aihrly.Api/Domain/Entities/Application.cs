using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain.Entities;

/// <summary>
/// Represents a candidate's application to a specific job.
/// 
/// Key rules:
/// - Starts at ApplicationStage.Applied when created
/// - Cannot have two applications with the same email for the same job
/// - Moves through stages via PATCH /api/applications/{id}/stage
/// </summary>
public class Application
{
    public Guid Id { get; set; }

    // Foreign key to Job
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;

    // Candidate info (supplied by the candidate themselves — no team member needed)
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CoverLetter { get; set; }

    // Current position in the hiring pipeline
    public ApplicationStage Stage { get; set; } = ApplicationStage.Applied;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    // Navigation collections
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();
    public ICollection<StageHistory> StageHistory { get; set; } = new List<StageHistory>();
    public ICollection<ApplicationScore> Scores { get; set; } = new List<ApplicationScore>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
