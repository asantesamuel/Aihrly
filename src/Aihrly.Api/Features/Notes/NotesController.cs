using Aihrly.Api.Common.Errors;
using Aihrly.Api.Common.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Aihrly.Api.Features.Notes;

[ApiController]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    // POST /api/applications/{id}/notes
    [HttpPost("api/applications/{id:guid}/notes")]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] AddNoteRequest request,
        CancellationToken ct)
    {
        var teamMemberId = GetRequiredTeamMemberId();

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new ValidationException("Type is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("Description is required.");

        var result = await _noteService.AddNoteAsync(id, teamMemberId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // GET /api/applications/{id}/notes
    [HttpGet("api/applications/{id:guid}/notes")]
    public async Task<IActionResult> ListNotes(Guid id, CancellationToken ct)
    {
        var result = await _noteService.ListNotesAsync(id, ct);
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