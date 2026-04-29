using Aihrly.Api.Common.Errors;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Common.Middleware;

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
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            var rawValue = headerValue.ToString().Trim();

            if (!Guid.TryParse(rawValue, out var teamMemberId))
            {
                throw new UnauthorizedException(
                    $"Header '{HeaderName}' must be a valid GUID. " +
                    $"Received: '{rawValue}'");
            }

            var exists = await db.TeamMembers
                .AnyAsync(t => t.Id == teamMemberId, context.RequestAborted);

            if (!exists)
            {
                throw new UnauthorizedException(
                    $"Team member with id '{teamMemberId}' does not exist.");
            }

            context.Items[ContextKey] = teamMemberId;
        }

        await _next(context);
    }
}
