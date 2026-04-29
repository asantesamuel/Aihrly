using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Aihrly.Api.Domain.Entities;

namespace Aihrly.Api.Features.Scores;

public class ScoreService : IScoreService
{
    private readonly AihrlyDbContext _db;

    public ScoreService(AihrlyDbContext db)
    {
        _db = db;
    }

    // -------------------------------------------------------------------------
    // PUT /api/applications/{id}/scores/{dimension}
    // -------------------------------------------------------------------------
    public async Task<ScoreResponse> SetScoreAsync(
        Guid applicationId,
        Guid teamMemberId,
        ScoreDimension dimension,
        SetScoreRequest request,
        CancellationToken ct = default)
    {
        if (request.Score < 1 || request.Score > 5)
            throw new ValidationException(
                $"Score must be between 1 and 5. Received: {request.Score}");

        var applicationExists = await _db.Applications
            .AnyAsync(a => a.Id == applicationId, ct);

        if (!applicationExists)
            throw new NotFoundException(nameof(Application), applicationId);

        var existing = await _db.ApplicationScores
            .FirstOrDefaultAsync(
                s => s.ApplicationId == applicationId &&
                     s.Dimension == dimension, ct);

        if (existing is null)
        {
            var score = new ApplicationScore
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Dimension = dimension,
                Score = request.Score,
                Comment = request.Comment,
                ScoredById = teamMemberId,
                ScoredAt = DateTime.UtcNow
            };

            _db.ApplicationScores.Add(score);
            await _db.SaveChangesAsync(ct);

            var scorer = await _db.TeamMembers.FindAsync([teamMemberId], ct);

            return new ScoreResponse(
                Dimension: dimension.ToString(),
                Score: score.Score,
                Comment: score.Comment,
                ScoredByName: scorer!.Name,
                ScoredAt: score.ScoredAt,
                UpdatedByName: null,
                UpdatedAt: null
            );
        }
        else
        {
            existing.Score = request.Score;
            existing.Comment = request.Comment;
            existing.UpdatedById = teamMemberId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var scorer = await _db.TeamMembers.FindAsync([existing.ScoredById], ct);
            var updater = await _db.TeamMembers.FindAsync([teamMemberId], ct);

            return new ScoreResponse(
                Dimension: dimension.ToString(),
                Score: existing.Score,
                Comment: existing.Comment,
                ScoredByName: scorer!.Name,
                ScoredAt: existing.ScoredAt,
                UpdatedByName: updater!.Name,
                UpdatedAt: existing.UpdatedAt
            );
        }
    }
}