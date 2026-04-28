using Aihrly.Api.Common.Errors;
using Aihrly.Api.Infrastructure.Persistence;

namespace Aihrly.Api.Common.Middleware;

/// <summary>
/// Resolves the X-Team-Member-Id header on every request.
/// 
/// HOW IT WORKS:
/// - Reads the header value
/// - If present, validates it is a valid Guid and that the team member exists in DB
/// - Stores the resolved TeamMember ID in HttpContext.Items so controllers can access it
/// - Does NOT throw if header is absent — some endpoints (e.g. GET /api/jobs,
///   POST applications) are public. Individual endpoints enforce the requirement.
/// 
/// IMPORTANT: The header value is stored as a Guid in HttpContext.Items["TeamMemberId"].
/// Services retrieve it via the ITeamMemberContext abstraction (defined separately).
/// </summary>
public class TeamMemberResolverMiddleware
{
    public const string HeaderName = "X-Team-Member-Id";
    public const string ContextKey = "TeamMemberId";

    private readonly RequestDelegate _next;

    public TeamMemberResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AihrlyDbContext db)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            if (!Guid.TryParse(headerValue, out var teamMemberId))
            {
                throw new UnauthorizedException(
                    $"Header '{HeaderName}' must be a valid GUID. Received: '{headerValue}'");
            }

            var exists = await db.TeamMembers.FindAsync(teamMemberId);
            if (exists is null)
            {
                throw new UnauthorizedException(
                    $"Team member with id '{teamMemberId}' does not exist.");
            }

            // Store the resolved ID so controllers and services can read it
            context.Items[ContextKey] = teamMemberId;
        }

        await _next(context);
    }
}
