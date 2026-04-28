using Aihrly.Api.Infrastructure.Persistence;

namespace Aihrly.Api.Features.Notes;

public class NoteService : INoteService
{
    private readonly AihrlyDbContext _db;

    public NoteService(AihrlyDbContext db)
    {
        _db = db;
    }

    public Task<NoteResponse> AddNoteAsync(
        Guid applicationId, Guid teamMemberId, AddNoteRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<NoteResponse>> ListNotesAsync(
        Guid applicationId, CancellationToken ct = default)
        => throw new NotImplementedException();
}
