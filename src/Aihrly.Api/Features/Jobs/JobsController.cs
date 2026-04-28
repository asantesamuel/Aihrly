using Aihrly.Api.Common.Errors;
using Aihrly.Api.Common.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Aihrly.Api.Features.Jobs;

/// <summary>
/// Handles all /api/jobs endpoints.
/// 
/// THIN CONTROLLER PRINCIPLE:
/// Controllers do three things only:
///   1. Extract inputs (route params, query params, body, headers)
///   2. Call the service
///   3. Return the HTTP response
/// 
/// No business logic lives here. If you find yourself writing an if-statement
/// about domain rules in a controller, move it to the service.
/// </summary>
[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    // POST /api/jobs
    [HttpPost]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateJobRequest request,
        CancellationToken ct)
    {
        var result = await _jobService.CreateJobAsync(request, ct);
        return CreatedAtAction(nameof(GetJob), new { id = result.Id }, result);
    }

    // GET /api/jobs?status=open&page=1&pageSize=20
    // PUBLIC — no X-Team-Member-Id required
    [HttpGet]
    public async Task<IActionResult> ListJobs(
        [FromQuery] string? status,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query  = new ListJobsQuery(status, page, pageSize);
        var result = await _jobService.ListJobsAsync(query, ct);
        return Ok(result);
    }

    // GET /api/jobs/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken ct)
    {
        var result = await _jobService.GetJobByIdAsync(id, ct);
        return Ok(result);
    }
}
