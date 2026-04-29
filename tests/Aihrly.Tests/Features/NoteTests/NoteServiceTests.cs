using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Notes;
using Aihrly.Api.Infrastructure.Persistence;

namespace Aihrly.Tests.Features.NoteTests;

public class NoteServiceTests
{
    private static AihrlyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AihrlyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AihrlyDbContext(options);
    }

    private static NoteService CreateService(AihrlyDbContext db) => new(db);

    private static async Task<(Guid jobId, Guid applicationId, Guid teamMemberId)>
        SeedAsync(AihrlyDbContext db)
    {
        var member = new TeamMember
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
        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            CandidateName = "Jane",
            CandidateEmail = "jane@example.com",
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow
        };

        db.TeamMembers.Add(member);
        db.Jobs.Add(job);
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        return (job.Id, application.Id, member.Id);
    }

    // -----------------------------------------------------------------------
    // AddNoteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddNote_ShouldPersistNote_AndReturnAuthorName()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (_, appId, memberId) = await SeedAsync(db);

        var result = await service.AddNoteAsync(
            appId, memberId,
            new AddNoteRequest("general", "Strong communication skills"));

        result.Type.Should().Be("General");
        result.Description.Should().Be("Strong communication skills");
        result.AuthorName.Should().Be("Alice Mensah");
        result.Id.Should().NotBe(Guid.Empty);

        var inDb = await db.ApplicationNotes.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.CreatedById.Should().Be(memberId);
    }

    [Fact]
    public async Task AddNote_ShouldThrowValidationException_ForInvalidNoteType()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (_, appId, memberId) = await SeedAsync(db);

        var act = async () => await service.AddNoteAsync(
            appId, memberId,
            new AddNoteRequest("invalid_type", "Some note"));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*not a valid note type*");
    }

    [Fact]
    public async Task AddNote_ShouldThrowNotFoundException_WhenApplicationDoesNotExist()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var act = async () => await service.AddNoteAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new AddNoteRequest("general", "Note"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("general")]
    [InlineData("screening")]
    [InlineData("interview")]
    [InlineData("referenceCheck")]
    [InlineData("redFlag")]
    public async Task AddNote_ShouldAcceptAllValidNoteTypes(string noteType)
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (_, appId, memberId) = await SeedAsync(db);

        var act = async () => await service.AddNoteAsync(
            appId, memberId,
            new AddNoteRequest(noteType, "Some description"));

        await act.Should().NotThrowAsync();
    }

    // -----------------------------------------------------------------------
    // ListNotesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ListNotes_ShouldReturnNotesNewestFirst()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (_, appId, memberId) = await SeedAsync(db);

        await service.AddNoteAsync(appId, memberId,
            new AddNoteRequest("general", "First note"));

        await Task.Delay(10); // ensure different timestamps

        await service.AddNoteAsync(appId, memberId,
            new AddNoteRequest("screening", "Second note"));

        var notes = await service.ListNotesAsync(appId);

        notes.Should().HaveCount(2);
        notes[0].Description.Should().Be("Second note"); // newest first
        notes[1].Description.Should().Be("First note");
    }

    [Fact]
    public async Task ListNotes_ShouldReturnEmpty_WhenNoNotesExist()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var (_, appId, _) = await SeedAsync(db);

        var notes = await service.ListNotesAsync(appId);

        notes.Should().BeEmpty();
    }

    [Fact]
    public async Task ListNotes_ShouldThrowNotFoundException_WhenApplicationDoesNotExist()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var act = async () => await service.ListNotesAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}