using Aihrly.Api.Features.Applications;
using Aihrly.Api.Features.Jobs;
using Aihrly.Api.Features.Notes;
using Aihrly.Api.Features.Notifications;
using Aihrly.Api.Features.Scores;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
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

            // Apply request examples, endpoint summaries, and team-member header docs.
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

    private static readonly Dictionary<string, (string Summary, string Description)> _operationDocs = new()
    {
        ["POST /api/jobs"] = (
            "Create a job",
            "Creates a new open job posting. This is a team-side action and requires X-Team-Member-Id."),

        ["GET /api/jobs"] = (
            "List jobs",
            "Returns paginated jobs. Optionally filter by status with ?status=open or ?status=closed."),

        ["GET /api/jobs/{id}"] = (
            "Get a job",
            "Returns one job posting by ID."),

        ["POST /api/jobs/{jobId}/applications"] = (
            "Submit an application",
            "Creates a candidate application for a job. New applications start in the Applied stage and this endpoint does not require X-Team-Member-Id."),

        ["GET /api/jobs/{jobId}/applications"] = (
            "List applications for a job",
            "Returns paginated applications for one job. Optionally filter by stage, for example ?stage=screening."),

        ["GET /api/applications/{id}"] = (
            "Get full applicant profile",
            "Returns the application, current stage, all notes, all scores, and full stage history in one response."),

        ["PATCH /api/applications/{id}/stage"] = (
            "Move an application to a new stage",
            "Validates the requested stage transition, updates the current stage, records stage history, and queues a notification if the new stage is Hired or Rejected."),

        ["POST /api/applications/{id}/notes"] = (
            "Add an application note",
            "Adds a note to an application. The author is resolved from X-Team-Member-Id, not from the request body."),

        ["GET /api/applications/{id}/notes"] = (
            "List application notes",
            "Returns notes for one application, newest first, including the author's name."),

        ["PUT /api/applications/{id}/scores/culture-fit"] = (
            "Set culture-fit score",
            "Creates or overwrites the culture-fit score for an application. Score must be between 1 and 5."),

        ["PUT /api/applications/{id}/scores/interview"] = (
            "Set interview score",
            "Creates or overwrites the interview score for an application. Score must be between 1 and 5."),

        ["PUT /api/applications/{id}/scores/assessment"] = (
            "Set assessment score",
            "Creates or overwrites the assessment score for an application. Score must be between 1 and 5.")
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Build the key: "METHOD /route/template"
        var method = context.ApiDescription.HttpMethod?.ToUpper() ?? "";
        var route = "/" + context.ApiDescription.RelativePath?.TrimEnd('/');

        var key = $"{method} {route}";

        ApplyOperationDocs(operation, key);
        ApplyRequestExample(operation, key);

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

    private static void ApplyOperationDocs(OpenApiOperation operation, string key)
    {
        if (!_operationDocs.TryGetValue(key, out var docs)) return;

        operation.Summary = docs.Summary;
        operation.Description = docs.Description;
    }

    private static void ApplyRequestExample(OpenApiOperation operation, string key)
    {
        if (operation.RequestBody?.Content is null) return;

        var example = key switch
        {
            "POST /api/jobs" => new OpenApiObject
            {
                ["title"] = new OpenApiString("Senior Backend Engineer"),
                ["description"] = new OpenApiString("Build and maintain hiring workflow APIs."),
                ["location"] = new OpenApiString("Accra")
            },

            "POST /api/jobs/{jobId}/applications" => new OpenApiObject
            {
                ["candidateName"] = new OpenApiString("Ama Mensah"),
                ["candidateEmail"] = new OpenApiString("ama.mensah@example.com"),
                ["coverLetter"] = new OpenApiString("I am excited about this role because I enjoy building reliable backend systems.")
            },

            "PATCH /api/applications/{id}/stage" => new OpenApiObject
            {
                ["targetStage"] = new OpenApiString("Interview"),
                ["reason"] = new OpenApiString("Candidate passed screening and should meet the hiring team.")
            },

            "POST /api/applications/{id}/notes" => new OpenApiObject
            {
                ["type"] = new OpenApiString("Screening"),
                ["description"] = new OpenApiString("Strong communication skills and relevant backend experience.")
            },

            "PUT /api/applications/{id}/scores/culture-fit" or
            "PUT /api/applications/{id}/scores/interview" or
            "PUT /api/applications/{id}/scores/assessment" => new OpenApiObject
            {
                ["score"] = new OpenApiInteger(4),
                ["comment"] = new OpenApiString("Good signal for this dimension.")
            },

            _ => null
        };

        if (example is null) return;

        foreach (var mediaType in operation.RequestBody.Content.Values)
        {
            mediaType.Example = example;
        }
    }
}
