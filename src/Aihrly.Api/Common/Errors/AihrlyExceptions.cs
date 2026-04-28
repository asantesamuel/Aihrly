namespace Aihrly.Api.Common.Errors;

/// <summary>
/// Base class for all domain-level exceptions in Aihrly.
/// Throwing a domain exception signals a known, expected failure
/// (bad input, rule violation) as opposed to an unexpected crash.
/// The global error middleware catches these and converts them to
/// the appropriate HTTP response with problem+json body.
/// </summary>
public abstract class AihrlyException : Exception
{
    protected AihrlyException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a requested resource does not exist.
/// Middleware converts this to HTTP 404.
/// 
/// Example: GET /api/jobs/{id} where the job doesn't exist,
/// or POST /api/jobs/{jobId}/applications where the job is closed
/// (we treat closed jobs as non-existent for candidates).
/// </summary>
public class NotFoundException : AihrlyException
{
    public NotFoundException(string resourceName, object id)
        : base($"{resourceName} with id '{id}' was not found.") { }
}

/// <summary>
/// Thrown when incoming data fails a business rule.
/// Middleware converts this to HTTP 400.
/// 
/// Examples:
/// - Invalid stage transition (Applied → Hired)
/// - Duplicate application (same email + same job)
/// - Missing or invalid X-Team-Member-Id header
/// </summary>
public class ValidationException : AihrlyException
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when X-Team-Member-Id header is missing or refers to
/// a team member that does not exist in the database.
/// Middleware converts this to HTTP 401.
/// </summary>
public class UnauthorizedException : AihrlyException
{
    public UnauthorizedException(string message) : base(message) { }
}
