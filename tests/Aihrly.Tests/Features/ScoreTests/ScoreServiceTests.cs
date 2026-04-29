using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Scores;
using Aihrly.Api.Infrastructure.Persistence;

namespace Aihrly.Tests.Features.ScoreTests;

public class ScoreServiceTests
{
    private static AihrlyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AihrlyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AihrlyDbContext(options);
    }

    private static ScoreService CreateService(AihrlyDbContext db) => new(db);

    private static async Task<(Guid applicationId, Guid memberId1, Guid memberId2)>
        SeedAsync(AihrlyDbContext db)
    {
        var member1 = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = "Alice Mensah",
            Email = "alice@aihrly.com",
            Role = TeamMemberRole.Recruiter
        };
        var member2 = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = "Bob Asante",
            Email = "bob@aihrly.com",
            Role = TeamMemberRole.Recruiter
        };
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Dev",
            Description = "D",
            Location = "L",
            Status = JobStatus.Open
        };
        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            CandidateName = "Jane",
            CandidateEmail = "jane@example.com",
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow
        };

        db.TeamMembers.AddRange(member1, member2);
        db.Jobs.Add(job);
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        return (application.Id, member1.Id, member2.Id);
    }

    // -----------------------------------------------------------------------
    // SetScoreAsync — first submission (INSERT)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetScore_FirstSubmission_ShouldInsertNewRow()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (appId, memberId1, _) = await SeedAsync(db);

        var result = await service.SetScoreAsync(
            appId, memberId1, ScoreDimension.CultureFit,
            new SetScoreRequest(4, "Good culture fit"));

        result.Score.Should().Be(4);
        result.Comment.Should().Be("Good culture fit");
        result.Dimension.Should().Be("CultureFit");
        result.ScoredByName.Should().Be("Alice Mensah");
        result.UpdatedByName.Should().BeNull();
        result.UpdatedAt.Should().BeNull();

        var inDb = await db.ApplicationScores
            .FirstOrDefaultAsync(s =>
                s.ApplicationId == appId &&
                s.Dimension == ScoreDimension.CultureFit);

        inDb.Should().NotBeNull();
        inDb!.Score.Should().Be(4);
    }

    // -----------------------------------------------------------------------
    // SetScoreAsync — second submission (UPDATE / overwrite)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetScore_SecondSubmission_ShouldOverwriteValue_AndTrackUpdatedBy()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (appId, memberId1, memberId2) = await SeedAsync(db);

        // First submission by Alice
        await service.SetScoreAsync(
            appId, memberId1, ScoreDimension.Interview,
            new SetScoreRequest(3, "Average"));

        // Second submission by Bob — should overwrite
        var result = await service.SetScoreAsync(
            appId, memberId2, ScoreDimension.Interview,
            new SetScoreRequest(5, "Excellent on reflection"));

        // Second value wins
        result.Score.Should().Be(5);
        result.Comment.Should().Be("Excellent on reflection");

        // Original scorer is preserved
        result.ScoredByName.Should().Be("Alice Mensah");

        // Updater is tracked
        result.UpdatedByName.Should().Be("Bob Asante");
        result.UpdatedAt.Should().NotBeNull();

        // Only ONE row exists — not two
        var rowCount = await db.ApplicationScores
            .CountAsync(s =>
                s.ApplicationId == appId &&
                s.Dimension == ScoreDimension.Interview);

        rowCount.Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // SetScoreAsync — three dimensions are independent
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetScore_ThreeDimensions_ShouldCreateThreeSeparateRows()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (appId, memberId1, _) = await SeedAsync(db);

        await service.SetScoreAsync(appId, memberId1,
            ScoreDimension.CultureFit, new SetScoreRequest(3, null));

        await service.SetScoreAsync(appId, memberId1,
            ScoreDimension.Interview, new SetScoreRequest(4, null));

        await service.SetScoreAsync(appId, memberId1,
            ScoreDimension.Assessment, new SetScoreRequest(5, null));

        var rows = await db.ApplicationScores
            .Where(s => s.ApplicationId == appId)
            .ToListAsync();

        rows.Should().HaveCount(3);
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task SetScore_ShouldThrowValidationException_WhenScoreOutOfRange(int score)
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (appId, memberId1, _) = await SeedAsync(db);

        var act = async () => await service.SetScoreAsync(
            appId, memberId1, ScoreDimension.CultureFit,
            new SetScoreRequest(score, null));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*between 1 and 5*");
    }

    [Fact]
    public async Task SetScore_ShouldThrowNotFoundException_WhenApplicationDoesNotExist()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var act = async () => await service.SetScoreAsync(
            Guid.NewGuid(), Guid.NewGuid(), ScoreDimension.CultureFit,
            new SetScoreRequest(3, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}