using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain.Entities;

/// <summary>
/// A note left on an application by a team member.
/// 
/// Security note: CreatedById always comes from the X-Team-Member-Id header,
/// never from the request body. Clients cannot claim to be someone else.
/// </summary>
public class ApplicationNote
{
    public Guid Id { get; set; }

    // Foreign key to Application
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public NoteType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    // Who wrote this note — resolved from X-Team-Member-Id header, not the request body
    public Guid CreatedById { get; set; }
    public TeamMember CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
