using Aihrly.Api.Features.Notifications;
using Aihrly.Api.Infrastructure.Persistence;

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

    public Task<ApplicationSummaryResponse> SubmitApplicationAsync(
        Guid jobId, SubmitApplicationRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PagedResult<ApplicationSummaryResponse>> ListApplicationsAsync(
        Guid jobId, ListApplicationsQuery query, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<ApplicationProfileResponse> GetApplicationProfileAsync(
        Guid applicationId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<ApplicationSummaryResponse> MoveStageAsync(
        Guid applicationId, Guid teamMemberId, MoveStageRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
}
