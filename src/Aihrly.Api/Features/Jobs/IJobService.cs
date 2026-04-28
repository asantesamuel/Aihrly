using Aihrly.Api.Features.Jobs;

namespace Aihrly.Api.Features.Jobs;

// ─── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Request body for creating a new job.</summary>
public record CreateJobRequest(
    string Title,
    string Description,
    string Location);

/// <summary>Query parameters for listing jobs.</summary>
public record ListJobsQuery(
    string? Status,   // optional filter: "open" or "closed"
    int Page     = 1,
    int PageSize = 20);

// ─── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Returned when listing or fetching a single job.</summary>
public record JobResponse(
    Guid   Id,
    string Title,
    string Description,
    string Location,
    string Status);

/// <summary>Paginated wrapper for job listings.</summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

// ─── Interface ────────────────────────────────────────────────────────────────

/// <summary>
/// Defines all operations the Jobs feature requires.
/// The controller depends only on this interface — never on the concrete service.
/// This means:
/// - We can swap implementations (e.g. add caching) without touching the controller
/// - Tests can mock this interface with Moq without needing a real database
/// </summary>
public interface IJobService
{
    Task<JobResponse> CreateJobAsync(CreateJobRequest request, CancellationToken ct = default);

    Task<PagedResult<JobResponse>> ListJobsAsync(ListJobsQuery query, CancellationToken ct = default);

    Task<JobResponse> GetJobByIdAsync(Guid id, CancellationToken ct = default);
}
