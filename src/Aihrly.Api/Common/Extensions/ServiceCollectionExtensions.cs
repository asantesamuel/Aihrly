using Aihrly.Api.Features.Applications;
using Aihrly.Api.Features.Jobs;
using Aihrly.Api.Features.Notes;
using Aihrly.Api.Features.Notifications;
using Aihrly.Api.Features.Scores;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Common.Extensions;

/// <summary>
/// Groups all service registrations into named extension methods.
/// Keeps Program.cs readable — each section of wiring lives here.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAihrlyDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AihrlyDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AihrlyDbContext).Assembly.FullName)
            )
        );

        return services;
    }

    public static IServiceCollection AddAihrlyServices(this IServiceCollection services)
    {
        // Feature services — scoped so they live for one HTTP request
        services.AddScoped<IJobService,         JobService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<INoteService,        NoteService>();
        services.AddScoped<IScoreService,       ScoreService>();

        // Notification queue — singleton so both producer and consumer share the same instance
        services.AddSingleton<NotificationQueue>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Background processor — hosted service (runs for the lifetime of the app)
        services.AddHostedService<BackgroundNotificationProcessor>();

        return services;
    }

    public static IServiceCollection AddAihrlySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title   = "Aihrly API",
                Version = "v1",
                Description = "Applicant Tracking System — team-side pipeline API"
            });

            // Tell Swagger about the X-Team-Member-Id header so it shows in the UI
            options.AddSecurityDefinition("TeamMemberId", new()
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                In   = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "X-Team-Member-Id",
                Description = "GUID of the acting team member. Required on all mutating endpoints except POST /api/jobs/{jobId}/applications."
            });
        });

        return services;
    }
}
