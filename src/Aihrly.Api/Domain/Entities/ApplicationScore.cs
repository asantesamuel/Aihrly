using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain.Entities;

/// <summary>
/// Stores a score for one dimension of one application.
/// 
/// Design decision: We use a separate table with one row per (ApplicationId, Dimension)
/// rather than columns on the Application table. This means:
/// - We can store who set the score and when (accountability)
/// - PUT semantics: upserting on (ApplicationId, Dimension) overwrites the previous value
/// 
/// Future improvement (noted in README): To track score history, add a
/// ScoreHistory table and insert instead of upsert.
/// </summary>
public class ApplicationScore
{
    public Guid Id { get; set; }

    // Foreign key to Application
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    // Which of the three scoring axes this row represents
    public ScoreDimension Dimension { get; set; }

    // Score must be between 1 and 5 (validated at API boundary)
    public int Score { get; set; }

    public string? Comment { get; set; }

    // Who set this score — from X-Team-Member-Id header
    public Guid ScoredById { get; set; }
    public TeamMember ScoredBy { get; set; } = null!;

    public DateTime ScoredAt { get; set; } = DateTime.UtcNow;

    // Populated when a score is overwritten (PUT semantics)
    public Guid? UpdatedById { get; set; }
    public TeamMember? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
