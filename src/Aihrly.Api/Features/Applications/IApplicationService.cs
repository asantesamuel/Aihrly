using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Features.Applications;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public record SubmitApplicationRequest(
    string  CandidateName,
    string  CandidateEmail,
    string? CoverLetter);

public record ListApplicationsQuery(
    string? Stage,      // optional filter e.g. "screening"
    int Page     = 1,
    int PageSize = 20);

public record MoveStageRequest(
    string  TargetStage,
    string? Reason);

// ─── Response DTOs ────────────────────────────────────────────────────────────

public record ApplicationSummaryResponse(
    Guid   Id,
    Guid   JobId,
    string CandidateName,
    string CandidateEmail,
    string Stage,
    DateTime AppliedAt);

/// <summary>
/// The full applicant profile — returned by GET /api/applications/{id}.
/// This is the richest response in the API. It combines:
/// - Basic application info
/// - All three scores (with scorer name)
/// - All notes (with author name)
/// - Full stage history (with actor name)
/// 
/// The goal is to give the frontend everything it needs in one request.
/// </summary>
public record ApplicationProfileResponse(
    Guid   Id,
    Guid   JobId,
    string CandidateName,
    string CandidateEmail,
    string? CoverLetter,
    string Stage,
    DateTime AppliedAt,
    IReadOnlyList<ScoreResponse>        Scores,
    IReadOnlyList<NoteResponse>         Notes,
    IReadOnlyList<StageHistoryResponse> StageHistory);

public record ScoreResponse(
    string   Dimension,
    int      Score,
    string?  Comment,
    string   ScoredByName,
    DateTime ScoredAt,
    string?  UpdatedByName,
    DateTime? UpdatedAt);

public record NoteResponse(
    Guid     Id,
    string   Type,
    string   Description,
    string   AuthorName,
    DateTime CreatedAt);

public record StageHistoryResponse(
    string   FromStage,
    string   ToStage,
    string   ChangedByName,
    DateTime ChangedAt,
    string?  Reason);

// ─── Interface ────────────────────────────────────────────────────────────────

public interface IApplicationService
{
    Task<ApplicationSummaryResponse> SubmitApplicationAsync(
        Guid jobId, SubmitApplicationRequest request, CancellationToken ct = default);

    Task<PagedResult<ApplicationSummaryResponse>> ListApplicationsAsync(
        Guid jobId, ListApplicationsQuery query, CancellationToken ct = default);

    Task<ApplicationProfileResponse> GetApplicationProfileAsync(
        Guid applicationId, CancellationToken ct = default);

    Task<ApplicationSummaryResponse> MoveStageAsync(
        Guid applicationId, Guid teamMemberId, MoveStageRequest request, CancellationToken ct = default);
}

// Reuse the paged result from Jobs
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
