using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Infrastructure.Persistence;

namespace Aihrly.Api.Features.Scores;

public class ScoreService : IScoreService
{
    private readonly AihrlyDbContext _db;

    public ScoreService(AihrlyDbContext db)
    {
        _db = db;
    }

    public Task<ScoreResponse> SetScoreAsync(
        Guid applicationId,
        Guid teamMemberId,
        ScoreDimension dimension,
        SetScoreRequest request,
        CancellationToken ct = default)
        => throw new NotImplementedException();
}
