using Aihrly.Api.Common.Errors;
using Aihrly.Api.Common.Middleware;
using Aihrly.Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Aihrly.Api.Features.Scores;

[ApiController]
public class ScoresController : ControllerBase
{
    private readonly IScoreService _scoreService;

    public ScoresController(IScoreService scoreService)
    {
        _scoreService = scoreService;
    }

    // PUT /api/applications/{id}/scores/culture-fit
    [HttpPut("api/applications/{id:guid}/scores/culture-fit")]
    public Task<IActionResult> SetCultureFitScore(
        Guid id, [FromBody] SetScoreRequest request, CancellationToken ct)
        => SetScore(id, ScoreDimension.CultureFit, request, ct);

    // PUT /api/applications/{id}/scores/interview
    [HttpPut("api/applications/{id:guid}/scores/interview")]
    public Task<IActionResult> SetInterviewScore(
        Guid id, [FromBody] SetScoreRequest request, CancellationToken ct)
        => SetScore(id, ScoreDimension.Interview, request, ct);

    // PUT /api/applications/{id}/scores/assessment
    [HttpPut("api/applications/{id:guid}/scores/assessment")]
    public Task<IActionResult> SetAssessmentScore(
        Guid id, [FromBody] SetScoreRequest request, CancellationToken ct)
        => SetScore(id, ScoreDimension.Assessment, request, ct);

    private async Task<IActionResult> SetScore(
        Guid applicationId, ScoreDimension dimension,
        SetScoreRequest request, CancellationToken ct)
    {
        var teamMemberId = GetRequiredTeamMemberId();

        var result = await _scoreService.SetScoreAsync(
            applicationId, teamMemberId, dimension, request, ct);

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