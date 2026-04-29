using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Applications;
using Aihrly.Api.Features.Notifications;
using Aihrly.Api.Infrastructure.Persistence;

namespace Aihrly.Tests.Features.ApplicationTests;

public class ApplicationServiceTests
{
    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------

    private static AihrlyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AihrlyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AihrlyDbContext(options);
    }

    private static (ApplicationService service, Mock<INotificationService> mock)
        CreateService(AihrlyDbContext db)
    {
        var mock = new Mock<INotificationService>();
        var service = new ApplicationService(db, mock.Object);
        return (service, mock);
    }

    private static async Task<Guid> SeedOpenJobAsync(AihrlyDbContext db)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Test Job",
            Description = "Desc",
            Location = "Accra",
            Status = JobStatus.Open
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        return job.Id;
    }

    private static async Task<Guid> SeedTeamMemberAsync(AihrlyDbContext db)
    {
        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = "Alice Mensah",
            Email = "alice@aihrly.com",
            Role = TeamMemberRole.Recruiter
        };
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();
        return member.Id;
    }

    // -----------------------------------------------------------------------
    // SubmitApplicationAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SubmitApplication_ShouldCreateApplication_WithAppliedStage()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);
        var jobId = await SeedOpenJobAsync(db);

        var result = await service.SubmitApplicationAsync(jobId,
            new SubmitApplicationRequest("Kwame Boateng", "kwame@example.com", "I love APIs"));

        result.Stage.Should().Be("Applied");
        result.CandidateName.Should().Be("Kwame Boateng");
        result.JobId.Should().Be(jobId);

        var inDb = await db.Applications.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Stage.Should().Be(ApplicationStage.Applied);
    }

    [Fact]
    public async Task SubmitApplication_ShouldThrowNotFoundException_WhenJobIsClosed()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Old Job",
            Description = "D",
            Location = "L",
            Status = JobStatus.Closed
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        var act = async () => await service.SubmitApplicationAsync(job.Id,
            new SubmitApplicationRequest("Jane", "jane@example.com", null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitApplication_ShouldThrowNotFoundException_WhenJobDoesNotExist()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);

        var act = async () => await service.SubmitApplicationAsync(Guid.NewGuid(),
            new SubmitApplicationRequest("Jane", "jane@example.com", null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitApplication_ShouldThrowValidationException_WhenDuplicateEmailForSameJob()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);
        var jobId = await SeedOpenJobAsync(db);

        await service.SubmitApplicationAsync(jobId,
            new SubmitApplicationRequest("Kwame", "kwame@example.com", null));

        var act = async () => await service.SubmitApplicationAsync(jobId,
            new SubmitApplicationRequest("Kwame Again", "kwame@example.com", null));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already applied*");
    }

    [Fact]
    public async Task SubmitApplication_ShouldAllowSameEmail_ForDifferentJobs()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);

        var job1 = new Job { Id = Guid.NewGuid(), Title = "J1", Description = "D", Location = "L", Status = JobStatus.Open };
        var job2 = new Job { Id = Guid.NewGuid(), Title = "J2", Description = "D", Location = "L", Status = JobStatus.Open };
        db.Jobs.AddRange(job1, job2);
        await db.SaveChangesAsync();

        await service.SubmitApplicationAsync(job1.Id,
            new SubmitApplicationRequest("Ama", "ama@example.com", null));

        var act = async () => await service.SubmitApplicationAsync(job2.Id,
            new SubmitApplicationRequest("Ama", "ama@example.com", null));

        await act.Should().NotThrowAsync();
    }

    // -----------------------------------------------------------------------
    // MoveStageAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MoveStage_ValidTransition_ShouldUpdateStageAndRecordHistory()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);
        var jobId = await SeedOpenJobAsync(db);
        var teamMemberId = await SeedTeamMemberAsync(db);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CandidateName = "Test",
            CandidateEmail = "t@t.com",
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow
        };
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        var result = await service.MoveStageAsync(
            application.Id, teamMemberId,
            new MoveStageRequest("Screening", "Looks good"));

        result.Stage.Should().Be("Screening");

        var inDb = await db.Applications.FindAsync(application.Id);
        inDb!.Stage.Should().Be(ApplicationStage.Screening);

        var history = await db.StageHistories
            .Where(sh => sh.ApplicationId == application.Id)
            .ToListAsync();

        history.Should().HaveCount(1);
        history[0].FromStage.Should().Be(ApplicationStage.Applied);
        history[0].ToStage.Should().Be(ApplicationStage.Screening);
        history[0].ChangedById.Should().Be(teamMemberId);
        history[0].Reason.Should().Be("Looks good");
    }

    [Fact]
    public async Task MoveStage_InvalidTransition_ShouldThrowValidationException()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);
        var jobId = await SeedOpenJobAsync(db);
        var teamMemberId = await SeedTeamMemberAsync(db);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CandidateName = "T",
            CandidateEmail = "t@t.com",
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow
        };
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        var act = async () => await service.MoveStageAsync(
            application.Id, teamMemberId,
            new MoveStageRequest("Hired", null));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Applied*");
    }

    [Fact]
    public async Task MoveStage_ToHired_ShouldEnqueueHiredNotification()
    {
        await using var db = CreateDb();
        var (service, mock) = CreateService(db);
        var jobId = await SeedOpenJobAsync(db);
        var teamMemberId = await SeedTeamMemberAsync(db);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CandidateName = "T",
            CandidateEmail = "t@t.com",
            Stage = ApplicationStage.Offer,
            AppliedAt = DateTime.UtcNow
        };
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        await service.MoveStageAsync(application.Id, teamMemberId,
            new MoveStageRequest("Hired", null));

        mock.Verify(
            n => n.EnqueueNotification(application.Id, NotificationType.Hired),
            Times.Once);
    }

    [Fact]
    public async Task MoveStage_ToNonTerminalStage_ShouldNotEnqueueNotification()
    {
        await using var db = CreateDb();
        var (service, mock) = CreateService(db);
        var jobId = await SeedOpenJobAsync(db);
        var teamMemberId = await SeedTeamMemberAsync(db);

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CandidateName = "T",
            CandidateEmail = "t@t.com",
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow
        };
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        await service.MoveStageAsync(application.Id, teamMemberId,
            new MoveStageRequest("Screening", null));

        mock.Verify(
            n => n.EnqueueNotification(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // GetApplicationProfileAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProfile_ShouldReturnAuthorName_NotJustId()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);

        var teamMember = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = "Alice Mensah",
            Email = "alice@aihrly.com",
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
        db.TeamMembers.Add(teamMember);
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            CandidateName = "Jane",
            CandidateEmail = "jane@example.com",
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow,
            Notes = new List<ApplicationNote>
            {
                new()
                {
                    Id          = Guid.NewGuid(),
                    Type        = NoteType.General,
                    Description = "Impressive CV",
                    CreatedById = teamMember.Id,
                    CreatedAt   = DateTime.UtcNow
                }
            }
        };
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        var profile = await service.GetApplicationProfileAsync(application.Id);

        profile.Notes.Should().HaveCount(1);
        profile.Notes[0].AuthorName.Should().Be("Alice Mensah");
        profile.Notes[0].Type.Should().Be("General");
        profile.Notes[0].Description.Should().Be("Impressive CV");
    }

    [Fact]
    public async Task GetProfile_ShouldThrowNotFoundException_WhenApplicationDoesNotExist()
    {
        await using var db = CreateDb();
        var (service, _) = CreateService(db);

        var act = async () =>
            await service.GetApplicationProfileAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }
}