namespace Aihrly.Api.Domain.Enums;

/// <summary>
/// The valid categories a note can belong to.
/// Using an enum here (as opposed to free text) means:
/// - Invalid note types are rejected at the API boundary
/// - Filtering notes by type is reliable and consistent
/// </summary>
public enum NoteType
{
    General,
    Screening,
    Interview,
    ReferenceCheck,
    RedFlag
}
