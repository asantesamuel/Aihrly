using Aihrly.Api.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Aihrly.Api.Features.Jobs;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateJobRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Title is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("Description is required.");

        if (string.IsNullOrWhiteSpace(request.Location))
            throw new ValidationException("Location is required.");

        var result = await _jobService.CreateJobAsync(request, ct);

        return CreatedAtAction(nameof(GetJob), new { id = result.Id }, result);
    }
    [HttpGet]
    public async Task<IActionResult> ListJobs(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = new ListJobsQuery(status, page, pageSize);
        var result = await _jobService.ListJobsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken ct)
    {
        var result = await _jobService.GetJobByIdAsync(id, ct);
        return Ok(result);
    }
}