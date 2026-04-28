using Aihrly.Api.Infrastructure.Persistence;

namespace Aihrly.Api.Features.Jobs;

/// <summary>
/// Concrete implementation of IJobService.
/// Stub only — methods will be implemented in the next phase.
/// 
/// DEPENDENCY INJECTION NOTE:
/// AihrlyDbContext is injected here (not newed up) because:
/// - It gives EF Core control over the connection lifetime
/// - It makes testing easy — tests inject a different DbContext (InMemory)
/// - It avoids hidden dependencies (no static state)
/// </summary>
public class JobService : IJobService
{
    private readonly AihrlyDbContext _db;

    public JobService(AihrlyDbContext db)
    {
        _db = db;
    }

    public Task<JobResponse> CreateJobAsync(CreateJobRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PagedResult<JobResponse>> ListJobsAsync(ListJobsQuery query, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<JobResponse> GetJobByIdAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
}
