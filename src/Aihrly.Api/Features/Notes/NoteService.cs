using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Features.Notes;

public class NoteService : INoteService
{
    private readonly AihrlyDbContext _db;

    public NoteService(AihrlyDbContext db)
    {
        _db = db;
    }

    // -------------------------------------------------------------------------
    // POST /api/applications/{id}/notes
    // -------------------------------------------------------------------------
    public async Task<NoteResponse> AddNoteAsync(
        Guid applicationId, Guid teamMemberId, AddNoteRequest request,
        CancellationToken ct = default)
    {
        var applicationExists = await _db.Applications
            .AnyAsync(a => a.Id == applicationId, ct);

        if (!applicationExists)
            throw new NotFoundException(nameof(Application), applicationId);

        if (!Enum.TryParse<NoteType>(request.Type, ignoreCase: true, out var noteType))
            throw new ValidationException(
                $"'{request.Type}' is not a valid note type. " +
                $"Valid types: {string.Join(", ", Enum.GetNames<NoteType>())}");

        var note = new ApplicationNote
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Type = noteType,
            Description = request.Description,
            CreatedById = teamMemberId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ApplicationNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        var author = await _db.TeamMembers.FindAsync([teamMemberId], ct);

        return new NoteResponse(
            Id: note.Id,
            Type: note.Type.ToString(),
            Description: note.Description,
            AuthorName: author!.Name,
            CreatedAt: note.CreatedAt
        );
    }

    // -------------------------------------------------------------------------
    // GET /api/applications/{id}/notes
    // -------------------------------------------------------------------------
    public async Task<IReadOnlyList<NoteResponse>> ListNotesAsync(
        Guid applicationId, CancellationToken ct = default)
    {
        var applicationExists = await _db.Applications
            .AnyAsync(a => a.Id == applicationId, ct);

        if (!applicationExists)
            throw new NotFoundException(nameof(Application), applicationId);

        var notes = await _db.ApplicationNotes
            .Where(n => n.ApplicationId == applicationId)
            .Include(n => n.CreatedBy)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return notes.Select(n => new NoteResponse(
            Id: n.Id,
            Type: n.Type.ToString(),
            Description: n.Description,
            AuthorName: n.CreatedBy.Name,
            CreatedAt: n.CreatedAt
        )).ToList();
    }
}