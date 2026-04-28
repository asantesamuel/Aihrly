using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain.Entities;

/// <summary>
/// An immutable record of a single stage transition for an application.
/// Every time PATCH /api/applications/{id}/stage is called successfully,
/// one row is inserted here. Rows are never updated or deleted.
/// </summary>
public class StageHistory
{
    public Guid Id { get; set; }

    // Foreign key to Application
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public ApplicationStage FromStage { get; set; }
    public ApplicationStage ToStage { get; set; }

    // Who triggered this transition — from X-Team-Member-Id header
    public Guid ChangedById { get; set; }
    public TeamMember ChangedBy { get; set; } = null!;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Optional reason provided in the PATCH body
    public string? Reason { get; set; }
}
