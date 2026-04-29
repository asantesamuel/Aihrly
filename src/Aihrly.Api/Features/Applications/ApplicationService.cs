using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Notifications;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Features.Applications;

public class ApplicationService : IApplicationService
{
    private readonly AihrlyDbContext _db;
    private readonly INotificationService _notifications;

    public ApplicationService(AihrlyDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    // -------------------------------------------------------------------------
    // POST /api/jobs/{jobId}/applications
    // -------------------------------------------------------------------------
    public async Task<ApplicationSummaryResponse> SubmitApplicationAsync(
        Guid jobId, SubmitApplicationRequest request, CancellationToken ct = default)
    {
        var job = await _db.Jobs.FindAsync([jobId], ct);

        if (job is null || job.Status == JobStatus.Closed)
            throw new NotFoundException(nameof(Job), jobId);

        var alreadyApplied = await _db.Applications.AnyAsync(
            a => a.JobId == jobId &&
                 a.CandidateEmail.ToLower() == request.CandidateEmail.ToLower(),
            ct);

        if (alreadyApplied)
            throw new ValidationException(
                $"A candidate with email '{request.CandidateEmail}' has already applied to this job.");

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CandidateName = request.CandidateName,
            CandidateEmail = request.CandidateEmail,
            CoverLetter = request.CoverLetter,
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync(ct);

        return MapToSummary(application);
    }

    // -------------------------------------------------------------------------
    // GET /api/jobs/{jobId}/applications
    // -------------------------------------------------------------------------
    public async Task<PagedResult<ApplicationSummaryResponse>> ListApplicationsAsync(
        Guid jobId, ListApplicationsQuery query, CancellationToken ct = default)
    {
        var jobExists = await _db.Jobs.AnyAsync(j => j.Id == jobId, ct);
        if (!jobExists)
            throw new NotFoundException(nameof(Job), jobId);

        var q = _db.Applications
            .Where(a => a.JobId == jobId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Stage) &&
            Enum.TryParse<ApplicationStage>(query.Stage, ignoreCase: true, out var parsedStage))
        {
            q = q.Where(a => a.Stage == parsedStage);
        }

        var totalCount = await q.CountAsync(ct);

        var applications = await q
            .OrderByDescending(a => a.AppliedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ApplicationSummaryResponse>(
            Items: applications.Select(MapToSummary).ToList(),
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize
        );
    }

    // -------------------------------------------------------------------------
    // GET /api/applications/{id}
    // -------------------------------------------------------------------------
    public async Task<ApplicationProfileResponse> GetApplicationProfileAsync(
        Guid applicationId, CancellationToken ct = default)
    {
        var application = await _db.Applications
            .Include(a => a.Notes)
                .ThenInclude(n => n.CreatedBy)
            .Include(a => a.StageHistory)
                .ThenInclude(sh => sh.ChangedBy)
            .Include(a => a.Scores)
                .ThenInclude(s => s.ScoredBy)
            .Include(a => a.Scores)
                .ThenInclude(s => s.UpdatedBy)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (application is null)
            throw new NotFoundException(nameof(Application), applicationId);

        var notes = application.Notes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteResponse(
                Id: n.Id,
                Type: n.Type.ToString(),
                Description: n.Description,
                AuthorName: n.CreatedBy.Name,
                CreatedAt: n.CreatedAt))
            .ToList();

        var history = application.StageHistory
            .OrderBy(sh => sh.ChangedAt)
            .Select(sh => new StageHistoryResponse(
                FromStage: sh.FromStage.ToString(),
                ToStage: sh.ToStage.ToString(),
                ChangedByName: sh.ChangedBy.Name,
                ChangedAt: sh.ChangedAt,
                Reason: sh.Reason))
            .ToList();

        var scores = application.Scores
            .Select(s => new ScoreResponse(
                Dimension: s.Dimension.ToString(),
                Score: s.Score,
                Comment: s.Comment,
                ScoredByName: s.ScoredBy.Name,
                ScoredAt: s.ScoredAt,
                UpdatedByName: s.UpdatedBy?.Name,
                UpdatedAt: s.UpdatedAt))
            .ToList();

        return new ApplicationProfileResponse(
            Id: application.Id,
            JobId: application.JobId,
            CandidateName: application.CandidateName,
            CandidateEmail: application.CandidateEmail,
            CoverLetter: application.CoverLetter,
            Stage: application.Stage.ToString(),
            AppliedAt: application.AppliedAt,
            Scores: scores,
            Notes: notes,
            StageHistory: history
        );
    }

    // -------------------------------------------------------------------------
    // PATCH /api/applications/{id}/stage
    // -------------------------------------------------------------------------
    public async Task<ApplicationSummaryResponse> MoveStageAsync(
        Guid applicationId, Guid teamMemberId, MoveStageRequest request,
        CancellationToken ct = default)
    {
        var application = await _db.Applications.FindAsync([applicationId], ct);
        if (application is null)
            throw new NotFoundException(nameof(Application), applicationId);

        if (!Enum.TryParse<ApplicationStage>(request.TargetStage, ignoreCase: true, out var targetStage))
            throw new ValidationException(
                $"'{request.TargetStage}' is not a valid stage. " +
                $"Valid stages: {string.Join(", ", Enum.GetNames<ApplicationStage>())}");

        if (!StageTransitionRules.IsValid(application.Stage, targetStage))
            throw new ValidationException(
                StageTransitionRules.GetErrorMessage(application.Stage, targetStage));

        var historyEntry = new StageHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            FromStage = application.Stage,
            ToStage = targetStage,
            ChangedById = teamMemberId,
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason
        };

        _db.StageHistories.Add(historyEntry);
        application.Stage = targetStage;

        await _db.SaveChangesAsync(ct);

        if (StageTransitionRules.IsTerminal(targetStage))
        {
            var notificationType = targetStage == ApplicationStage.Hired
                ? NotificationType.Hired
                : NotificationType.Rejected;

            _notifications.EnqueueNotification(applicationId, notificationType);
        }

        return MapToSummary(application);
    }

    // -------------------------------------------------------------------------
    // PRIVATE HELPERS
    // -------------------------------------------------------------------------
    private static ApplicationSummaryResponse MapToSummary(Application a) => new(
        Id: a.Id,
        JobId: a.JobId,
        CandidateName: a.CandidateName,
        CandidateEmail: a.CandidateEmail,
        Stage: a.Stage.ToString(),
        AppliedAt: a.AppliedAt
    );
}