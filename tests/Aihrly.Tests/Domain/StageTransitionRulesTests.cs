using Xunit;
using Aihrly.Api.Domain;
using Aihrly.Api.Domain.Enums;
using FluentAssertions;


namespace Aihrly.Tests.Domain;

/// <summary>
/// Unit tests for StageTransitionRules.
/// 
/// WHY NO MOCKS HERE:
/// StageTransitionRules is a static class with zero dependencies —
/// no database, no HTTP, no services. It is pure logic that takes
/// inputs and returns outputs. Mocking is only needed when a class
/// depends on something external (a database, a clock, an HTTP client).
/// Here, we test directly.
/// 
/// TDD NOTE: These tests were written before the implementation was
/// trusted. They define the contract. If the rules change, these
/// tests break — which is exactly what we want.
/// </summary>
public class StageTransitionRulesTests
{
    // -------------------------------------------------------------------------
    // VALID transitions — should return true
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Screening)]
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Rejected)]
    [InlineData(ApplicationStage.Screening, ApplicationStage.Interview)]
    [InlineData(ApplicationStage.Screening, ApplicationStage.Rejected)]
    [InlineData(ApplicationStage.Interview, ApplicationStage.Offer)]
    [InlineData(ApplicationStage.Interview, ApplicationStage.Rejected)]
    [InlineData(ApplicationStage.Offer,     ApplicationStage.Hired)]
    [InlineData(ApplicationStage.Offer,     ApplicationStage.Rejected)]
    public void IsValid_ShouldReturnTrue_ForAllowedTransitions(
        ApplicationStage from, ApplicationStage to)
    {
        // Act
        var result = StageTransitionRules.IsValid(from, to);

        // Assert
        result.Should().BeTrue(
            because: $"'{from}' → '{to}' is a documented valid transition");
    }

    // -------------------------------------------------------------------------
    // INVALID transitions — should return false
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Hired)]    // skipping stages
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Offer)]    // skipping stages
    [InlineData(ApplicationStage.Screening, ApplicationStage.Hired)]    // skipping stages
    [InlineData(ApplicationStage.Hired,     ApplicationStage.Rejected)] // terminal stage
    [InlineData(ApplicationStage.Rejected,  ApplicationStage.Interview)]// terminal stage
    [InlineData(ApplicationStage.Hired,     ApplicationStage.Hired)]    // self-transition
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Applied)]  // self-transition
    public void IsValid_ShouldReturnFalse_ForDisallowedTransitions(
        ApplicationStage from, ApplicationStage to)
    {
        // Act
        var result = StageTransitionRules.IsValid(from, to);

        // Assert
        result.Should().BeFalse(
            because: $"'{from}' → '{to}' is not a documented valid transition");
    }

    // -------------------------------------------------------------------------
    // Terminal stage detection
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(ApplicationStage.Hired)]
    [InlineData(ApplicationStage.Rejected)]
    public void IsTerminal_ShouldReturnTrue_ForTerminalStages(ApplicationStage stage)
    {
        StageTransitionRules.IsTerminal(stage).Should().BeTrue();
    }

    [Theory]
    [InlineData(ApplicationStage.Applied)]
    [InlineData(ApplicationStage.Screening)]
    [InlineData(ApplicationStage.Interview)]
    [InlineData(ApplicationStage.Offer)]
    public void IsTerminal_ShouldReturnFalse_ForNonTerminalStages(ApplicationStage stage)
    {
        StageTransitionRules.IsTerminal(stage).Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Error message — should be meaningful, not empty
    // -------------------------------------------------------------------------

    [Fact]
    public void GetErrorMessage_ShouldMentionBothStages_WhenTransitionIsInvalid()
    {
        // Arrange
        var from = ApplicationStage.Applied;
        var to   = ApplicationStage.Hired;

        // Act
        var message = StageTransitionRules.GetErrorMessage(from, to);

        // Assert
        message.Should().Contain("Applied");
        message.Should().Contain("Hired");
        message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetErrorMessage_ShouldMentionTerminal_WhenFromStageIsTerminal()
    {
        var message = StageTransitionRules.GetErrorMessage(
            ApplicationStage.Hired, ApplicationStage.Rejected);

        message.Should().Contain("terminal");
    }
}
