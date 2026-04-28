namespace Aihrly.Api.Domain.Enums;

/// <summary>
/// Defines the two valid roles a team member can hold.
/// Using an enum prevents invalid roles from ever entering the system
/// (e.g. typos like "Recruter" or casing differences like "RECRUITER").
/// </summary>
public enum TeamMemberRole
{
    Recruiter,
    HiringManager
}
