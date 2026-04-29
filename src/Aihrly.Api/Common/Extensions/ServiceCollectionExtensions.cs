using Aihrly.Api.Features.Applications;
using Aihrly.Api.Features.Jobs;
using Aihrly.Api.Features.Notes;
using Aihrly.Api.Features.Notifications;
using Aihrly.Api.Features.Scores;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Aihrly.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAihrlyDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AihrlyDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(AihrlyDbContext).Assembly.FullName)
            )
        );

        return services;
    }

    public static IServiceCollection AddAihrlyServices(
        this IServiceCollection services)
    {
        // Scoped — one instance per HTTP request
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IScoreService, ScoreService>();

        // Singleton — one instance for the entire app lifetime.
        // NotificationQueue MUST be singleton so the background processor
        // and the notification service share the exact same channel instance.
        services.AddSingleton<NotificationQueue>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Hosted service — starts with the app, shares the singleton queue
        services.AddHostedService<BackgroundNotificationProcessor>();

        return services;
    }

    public static IServiceCollection AddAihrlySwagger(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Aihrly API",
                Version = "v1",
                Description =
                    "Applicant Tracking System — team-side pipeline.\n\n" +
                    "**Seeded team member IDs for X-Team-Member-Id header:**\n\n" +
                    "- Alice Mensah (Recruiter):     `00000000-0000-0000-0000-000000000001`\n" +
                    "- Bob Asante (Recruiter):       `00000000-0000-0000-0000-000000000002`\n" +
                    "- Carol Owusu (HiringManager):  `00000000-0000-0000-0000-000000000003`"
            });

            // Register the header as a security scheme
            options.AddSecurityDefinition("TeamMemberId", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Team-Member-Id",
                Description = "GUID of the acting team member.\n\n" +
                              "Alice:  00000000-0000-0000-0000-000000000001\n" +
                              "Bob:    00000000-0000-0000-0000-000000000002\n" +
                              "Carol:  00000000-0000-0000-0000-000000000003"
            });

            // Apply the header as an explicit input field on every endpoint
            options.OperationFilter<TeamMemberHeaderOperationFilter>();
        });

        return services;
    }
}

/// <summary>
/// Adds X-Team-Member-Id ONLY to endpoints that actually require it.
/// Public endpoints (GET jobs, GET applications, POST application) are excluded.
/// </summary>
public class TeamMemberHeaderOperationFilter : IOperationFilter
{
    // These route + method combinations do NOT need the header
    private static readonly HashSet<string> _publicEndpoints = new()
    {
        "GET /api/jobs",
        "GET /api/jobs/{id}",
        "GET /api/jobs/{jobId}/applications",
        "GET /api/applications/{id}",
        "GET /api/applications/{id}/notes",
        "POST /api/jobs/{jobId}/applications"
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Build the key: "METHOD /route/template"
        var method = context.ApiDescription.HttpMethod?.ToUpper() ?? "";
        var route = "/" + context.ApiDescription.RelativePath?.TrimEnd('/');

        var key = $"{method} {route}";

        // Skip public endpoints
        if (_publicEndpoints.Contains(key)) return;

        operation.Parameters ??= new List<OpenApiParameter>();

        var alreadyAdded = operation.Parameters
            .Any(p => p.Name == "X-Team-Member-Id" &&
                      p.In == ParameterLocation.Header);

        if (alreadyAdded) return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Team-Member-Id",
            In = ParameterLocation.Header,
            Required = true,
            Description = "GUID of the acting team member.\n\n" +
                          "Alice (Recruiter):      00000000-0000-0000-0000-000000000001\n" +
                          "Bob   (Recruiter):      00000000-0000-0000-0000-000000000002\n" +
                          "Carol (HiringManager):  00000000-0000-0000-0000-000000000003",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });
    }
}