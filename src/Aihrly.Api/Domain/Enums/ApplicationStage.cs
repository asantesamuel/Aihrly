namespace Aihrly.Api.Domain.Enums;

/// <summary>
/// Represents every stage in the hiring pipeline.
/// 
/// Valid transitions (enforced by StageTransitionRules):
///   Applied    → Screening | Rejected
///   Screening  → Interview | Rejected
///   Interview  → Offer     | Rejected
///   Offer      → Hired     | Rejected
///   Hired      → (terminal — no further moves)
///   Rejected   → (terminal — no further moves)
/// </summary>
public enum ApplicationStage
{
    Applied,
    Screening,
    Interview,
    Offer,
    Hired,
    Rejected
}
