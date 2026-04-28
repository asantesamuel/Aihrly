using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Features.Scores;

// ─── Request / Response DTOs ──────────────────────────────────────────────────

public record SetScoreRequest(
    int     Score,    // must be 1–5, validated at API boundary
    string? Comment);

public record ScoreResponse(
    string   Dimension,
    int      Score,
    string?  Comment,
    string   ScoredByName,
    DateTime ScoredAt,
    string?  UpdatedByName,
    DateTime? UpdatedAt);

// ─── Interface ────────────────────────────────────────────────────────────────

/// <summary>
/// Handles all three score endpoints.
/// The dimension (CultureFit / Interview / Assessment) is passed as a parameter
/// rather than having three separate service methods — the logic is identical,
/// only the dimension differs.
/// </summary>
public interface IScoreService
{
    Task<ScoreResponse> SetScoreAsync(
        Guid applicationId,
        Guid teamMemberId,
        ScoreDimension dimension,
        SetScoreRequest request,
        CancellationToken ct = default);
}
