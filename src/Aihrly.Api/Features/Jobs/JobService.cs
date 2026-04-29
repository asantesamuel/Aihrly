using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Features.Jobs;

public class JobService : IJobService
{
    private readonly AihrlyDbContext _db;

    public JobService(AihrlyDbContext db)
    {
        _db = db;
    }

    public async Task<JobResponse> CreateJobAsync(
        CreateJobRequest request, CancellationToken ct = default)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            Status = JobStatus.Open
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync(ct);

        return MapToResponse(job);
    }

    public async Task<PagedResult<JobResponse>> ListJobsAsync(
        ListJobsQuery query, CancellationToken ct = default)
    {
        var q = _db.Jobs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<JobStatus>(query.Status, ignoreCase: true, out var parsedStatus))
        {
            q = q.Where(j => j.Status == parsedStatus);
        }

        var totalCount = await q.CountAsync(ct);

        var jobs = await q
            .OrderBy(j => j.Title)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<JobResponse>(
            Items: jobs.Select(MapToResponse).ToList(),
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize
        );
    }

    public async Task<JobResponse> GetJobByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        var job = await _db.Jobs.FindAsync([id], ct);

        if (job is null)
            throw new NotFoundException(nameof(Job), id);

        return MapToResponse(job);
    }

    private static JobResponse MapToResponse(Job job) => new(
        Id: job.Id,
        Title: job.Title,
        Description: job.Description,
        Location: job.Location,
        Status: job.Status.ToString()
    );
}
