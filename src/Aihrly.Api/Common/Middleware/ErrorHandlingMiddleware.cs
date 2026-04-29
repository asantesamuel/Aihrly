using System.Text.Json;
using Aihrly.Api.Common.Errors;

namespace Aihrly.Api.Common.Middleware;

/// <summary>
/// Global error handling middleware.
/// 
/// WHY MIDDLEWARE INSTEAD OF CONTROLLER TRY/CATCH:
/// If every controller had its own try/catch, error handling logic
/// would be duplicated across 10+ action methods. This middleware
/// sits at the top of the pipeline and catches any unhandled exception
/// from any layer below it (controllers, services, repositories).
/// 
/// It converts domain exceptions into consistent problem+json responses.
/// The shape is always: { "status": 400, "title": "...", "detail": "..." }
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // TEMPORARY DEBUG — remove after confirming it works
            var allHeaders = string.Join(", ", context.Request.Headers.Keys);
            Console.WriteLine($"[DEBUG] Headers received: {allHeaders}");
            Console.WriteLine($"[DEBUG] Items after middleware: {string.Join(", ", context.Items.Keys)}");

            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected crash — log full details but return a safe generic message
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error", "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            status = statusCode,
            title,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
