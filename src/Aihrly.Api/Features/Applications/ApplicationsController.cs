using Aihrly.Api.Common.Errors;
using Aihrly.Api.Common.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Aihrly.Api.Features.Applications;

[ApiController]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    // POST /api/jobs/{jobId}/applications
    // PUBLIC — candidates apply here, no X-Team-Member-Id needed
    [HttpPost("api/jobs/{jobId:guid}/applications")]
    public async Task<IActionResult> SubmitApplication(
        Guid jobId,
        [FromBody] SubmitApplicationRequest request,
        CancellationToken ct)
    {
        var result = await _applicationService.SubmitApplicationAsync(jobId, request, ct);
        return CreatedAtAction(nameof(GetApplication), new { id = result.Id }, result);
    }

    // GET /api/jobs/{jobId}/applications?stage=screening
    [HttpGet("api/jobs/{jobId:guid}/applications")]
    public async Task<IActionResult> ListApplications(
        Guid jobId,
        [FromQuery] string? stage,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query  = new ListApplicationsQuery(stage, page, pageSize);
        var result = await _applicationService.ListApplicationsAsync(jobId, query, ct);
        return Ok(result);
    }

    // GET /api/applications/{id}
    [HttpGet("api/applications/{id:guid}")]
    public async Task<IActionResult> GetApplication(Guid id, CancellationToken ct)
    {
        var result = await _applicationService.GetApplicationProfileAsync(id, ct);
        return Ok(result);
    }

    // PATCH /api/applications/{id}/stage
    [HttpPatch("api/applications/{id:guid}/stage")]
    public async Task<IActionResult> MoveStage(
        Guid id,
        [FromBody] MoveStageRequest request,
        CancellationToken ct)
    {
        // Require X-Team-Member-Id — reject if missing or invalid
        var teamMemberId = GetRequiredTeamMemberId();
        var result = await _applicationService.MoveStageAsync(id, teamMemberId, request, ct);
        return Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the resolved TeamMemberId from HttpContext.Items (set by middleware).
    /// Throws UnauthorizedException if the header was not present on this request.
    /// </summary>
    private Guid GetRequiredTeamMemberId()
    {
        if (HttpContext.Items.TryGetValue(TeamMemberResolverMiddleware.ContextKey, out var value)
            && value is Guid id)
        {
            return id;
        }

        throw new UnauthorizedException(
            $"This endpoint requires the '{TeamMemberResolverMiddleware.HeaderName}' header.");
    }
}
