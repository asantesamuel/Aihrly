using Aihrly.Api.Common.Extensions;
using Aihrly.Api.Common.Middleware;
using Aihrly.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings ("Open" not 0) in all responses
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Database
builder.Services.AddAihrlyDatabase(builder.Configuration);

// Feature services + background job
builder.Services.AddAihrlyServices();

// Swagger
builder.Services.AddAihrlySwagger();

// ── Build ─────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware pipeline (ORDER MATTERS) ───────────────────────────────────────

// 1. Error handling catches exceptions from everything below it
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2. Resolve X-Team-Member-Id header on every request
app.UseMiddleware<TeamMemberResolverMiddleware>();

// 3. Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aihrly API v1"));
}

// 4. Route to controllers
app.UseRouting();
app.MapControllers();

// ── Apply migrations on startup ───────────────────────────────────────────────

await ApplyMigrationsAsync(app);

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AihrlyDbContext>();
    await db.Database.MigrateAsync();
}

public partial class Program { }