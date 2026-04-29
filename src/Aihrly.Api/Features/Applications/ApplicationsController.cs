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
    [HttpPost("api/jobs/{jobId:guid}/applications")]
    public async Task<IActionResult> SubmitApplication(
        Guid jobId,
        [FromBody] SubmitApplicationRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateName))
            throw new ValidationException("CandidateName is required.");

        if (string.IsNullOrWhiteSpace(request.CandidateEmail))
            throw new ValidationException("CandidateEmail is required.");

        if (!request.CandidateEmail.Contains('@') ||
            !request.CandidateEmail.Contains('.'))
            throw new ValidationException(
                $"'{request.CandidateEmail}' is not a valid email address.");

        var result = await _applicationService.SubmitApplicationAsync(jobId, request, ct);
        return CreatedAtAction(nameof(GetApplication), new { id = result.Id }, result);
    }

    // GET /api/jobs/{jobId}/applications?stage=screening&page=1&pageSize=20
    [HttpGet("api/jobs/{jobId:guid}/applications")]
    public async Task<IActionResult> ListApplications(
        Guid jobId,
        [FromQuery] string? stage,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = new ListApplicationsQuery(stage, page, pageSize);
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
        var teamMemberId = GetRequiredTeamMemberId();

        if (string.IsNullOrWhiteSpace(request.TargetStage))
            throw new ValidationException("TargetStage is required.");

        var result = await _applicationService.MoveStageAsync(id, teamMemberId, request, ct);
        return Ok(result);
    }

    private Guid GetRequiredTeamMemberId()
    {
        if (HttpContext.Items.TryGetValue(TeamMemberResolverMiddleware.ContextKey, out var value)
            && value is Guid id)
            return id;

        throw new UnauthorizedException(
            $"This endpoint requires the '{TeamMemberResolverMiddleware.HeaderName}' header.");
    }
}