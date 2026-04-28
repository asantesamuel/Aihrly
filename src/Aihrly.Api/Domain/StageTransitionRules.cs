using Aihrly.Api.Domain.Enums;

namespace Aihrly.Api.Domain;

/// <summary>
/// Encapsulates the hiring pipeline transition rules.
/// 
/// This is pure domain logic — no database, no HTTP, no EF Core.
/// It takes a current stage and a target stage, and tells you
/// whether the move is allowed.
/// 
/// Keeping this as a static class with no dependencies means:
/// - It is trivially unit testable (no mocks needed)
/// - Services can call it without injecting anything
/// - The rules live in one place and nowhere else
/// </summary>
public static class StageTransitionRules
{
    /// <summary>
    /// Maps each stage to the set of stages it is allowed to transition into.
    /// Hired and Rejected are terminal — they map to empty sets.
    /// </summary>
    private static readonly Dictionary<ApplicationStage, HashSet<ApplicationStage>> _allowedTransitions = new()
    {
        [ApplicationStage.Applied]    = [ApplicationStage.Screening, ApplicationStage.Rejected],
        [ApplicationStage.Screening]  = [ApplicationStage.Interview, ApplicationStage.Rejected],
        [ApplicationStage.Interview]  = [ApplicationStage.Offer,     ApplicationStage.Rejected],
        [ApplicationStage.Offer]      = [ApplicationStage.Hired,     ApplicationStage.Rejected],
        [ApplicationStage.Hired]      = [],
        [ApplicationStage.Rejected]   = [],
    };

    /// <summary>
    /// Returns true if moving from <paramref name="from"/> to <paramref name="to"/> is a valid transition.
    /// </summary>
    public static bool IsValid(ApplicationStage from, ApplicationStage to)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    /// <summary>
    /// Returns true if the given stage is terminal (no further moves allowed).
    /// </summary>
    public static bool IsTerminal(ApplicationStage stage)
    {
        return stage is ApplicationStage.Hired or ApplicationStage.Rejected;
    }

    /// <summary>
    /// Returns a human-readable error message for an invalid transition.
    /// Used in the 400 response body.
    /// </summary>
    public static string GetErrorMessage(ApplicationStage from, ApplicationStage to)
    {
        if (IsTerminal(from))
            return $"Cannot move application from '{from}' — it is a terminal stage and accepts no further transitions.";

        return $"Transition from '{from}' to '{to}' is not allowed. " +
               $"Valid next stages are: {string.Join(", ", _allowedTransitions[from])}.";
    }
}
