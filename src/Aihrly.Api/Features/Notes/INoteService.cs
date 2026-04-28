namespace Aihrly.Api.Features.Notes;

// ─── Request / Response DTOs ──────────────────────────────────────────────────

public record AddNoteRequest(
    string Type,
    string Description);

public record NoteResponse(
    Guid     Id,
    string   Type,
    string   Description,
    string   AuthorName,  // resolved name, not just ID
    DateTime CreatedAt);

// ─── Interface ────────────────────────────────────────────────────────────────

public interface INoteService
{
    Task<NoteResponse> AddNoteAsync(
        Guid applicationId, Guid teamMemberId, AddNoteRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<NoteResponse>> ListNotesAsync(
        Guid applicationId, CancellationToken ct = default);
}
