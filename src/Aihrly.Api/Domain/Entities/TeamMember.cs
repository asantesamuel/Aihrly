using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain.Entities;

/// <summary>
/// Represents a member of the hiring team (recruiter or hiring manager).
/// TeamMembers are seeded at startup — there is no registration endpoint.
/// Every mutating request identifies the acting member via X-Team-Member-Id header.
/// </summary>
public class TeamMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; }

    // Navigation: a team member can author many notes
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();

    // Navigation: a team member can change many stages
    public ICollection<StageHistory> StageChanges { get; set; } = new List<StageHistory>();

    // Navigation: a team member can set many scores
    public ICollection<ApplicationScore> Scores { get; set; } = new List<ApplicationScore>();
}
