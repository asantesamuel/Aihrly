namespace Aihrly.Api.Domain.Enums;

/// <summary>
/// The three dimensions on which a candidate can be scored.
/// Stored as a column in the ApplicationScore table so each
/// dimension has exactly one row per application (overwrite semantics).
/// </summary>
public enum ScoreDimension
{
    CultureFit,
    Interview,
    Assessment
}
