using Xunit;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Scores;
using Aihrly.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Tests.Features.Scores;


/// <summary>
/// Tests for ScoreService — verifying PUT (overwrite) semantics.
/// 
/// WHY INMEMORY DB:
/// Score overwrite logic requires checking whether a row already exists
/// for (ApplicationId, Dimension), then either inserting or updating.
/// This is real database interaction — mocking it would not test the
/// actual upsert logic. InMemory lets us test this without PostgreSQL.
/// </summary>
public class ScoreServiceTests
{
    private AihrlyDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AihrlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AihrlyDbContext(options);
    }

    [Fact]
    public async Task SetScore_SubmittedTwice_SecondValueShouldWin_AndUpdatedByTracked()
    {
        // Arrange
        await using var db = CreateInMemoryDb();

        var scorer1 = new TeamMember
            { Id = Guid.NewGuid(), Name = "Alice", Email = "a@a.com", Role = TeamMemberRole.Recruiter };
        var scorer2 = new TeamMember
            { Id = Guid.NewGuid(), Name = "Bob",   Email = "b@b.com", Role = TeamMemberRole.Recruiter };
        var application = new Application
        {
            Id = Guid.NewGuid(), JobId = Guid.NewGuid(),
            CandidateName = "Test", CandidateEmail = "t@t.com",
            Stage = ApplicationStage.Applied, AppliedAt = DateTime.UtcNow
        };

        db.TeamMembers.AddRange(scorer1, scorer2);
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        // TODO: var service = new ScoreService(db);

        // Act — first submission
        // await service.SetScoreAsync(application.Id, scorer1.Id,
        //     ScoreDimension.CultureFit, new SetScoreRequest(3, "Average"));

        // Act — second submission (should overwrite)
        // var result = await service.SetScoreAsync(application.Id, scorer2.Id,
        //     ScoreDimension.CultureFit, new SetScoreRequest(5, "Excellent on reflection"));

        // Assert — second value wins
        // result.Score.Should().Be(5);
        // result.Comment.Should().Be("Excellent on reflection");
        // result.ScoredByName.Should().Be("Alice");   // original scorer preserved
        // result.UpdatedByName.Should().Be("Bob");     // updater tracked
        // result.UpdatedAt.Should().NotBeNull();

        // Verify only ONE row exists in DB (not two)
        // var scoreRows = await db.ApplicationScores
        //     .Where(s => s.ApplicationId == application.Id && s.Dimension == ScoreDimension.CultureFit)
        //     .ToListAsync();
        // scoreRows.Should().HaveCount(1);

        // SCAFFOLD: uncomment when ScoreService is implemented
        await Task.CompletedTask;
    }
}
