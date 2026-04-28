using Aihrly.Api.Common.Extensions;
using Aihrly.Api.Common.Middleware;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// Database (PostgreSQL via EF Core + Npgsql)
builder.Services.AddAihrlyDatabase(builder.Configuration);

// Feature services (Jobs, Applications, Notes, Scores, Notifications)
builder.Services.AddAihrlyServices();

// Swagger / OpenAPI
builder.Services.AddAihrlySwagger();

// ── Build app ─────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware pipeline (ORDER MATTERS) ───────────────────────────────────────

// 1. Error handling must be FIRST so it catches exceptions from everything below
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2. Resolve X-Team-Member-Id header early so all controllers can use it
app.UseMiddleware<TeamMemberResolverMiddleware>();

// 3. Swagger UI (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aihrly API v1"));
}

// 4. Route to controllers
app.UseRouting();
app.MapControllers();

// ── Database migration + seeding on startup ───────────────────────────────────

await ApplyMigrationsAsync(app);

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AihrlyDbContext>();

    // Apply any pending migrations automatically on startup.
    // Seed data (team members) is applied via HasData in EF configurations.
    await db.Database.MigrateAsync();
}

// Make Program accessible to test projects for integration testing
public partial class Program { }
