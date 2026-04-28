using Xunit;
using Aihrly.Api.Common.Errors;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Applications;
using Aihrly.Api.Features.Notes;
using Aihrly.Api.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Tests.Features.Applications;




/// <summary>
/// Integration-style tests for ApplicationService.
/// 
/// WHY WE USE EF CORE INMEMORY HERE (instead of Moq):
/// These tests verify behaviour that depends on the database — duplicate
/// detection, data persistence, and join queries (author name on notes).
/// Mocking the DbContext would make these tests meaningless because we'd
/// be testing our mock, not the real query logic.
/// 
/// InMemory gives us a real EF Core context with no PostgreSQL needed —
/// perfect for CI and local test runs.
/// 
/// LIMITATION: InMemory doesn't enforce unique constraints. For the
/// duplicate-application test we validate at the service layer (which is
/// where that check should live anyway).
/// </summary>
public class ApplicationServiceTests
{
    private AihrlyDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AihrlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // fresh DB per test
            .Options;

        return new AihrlyDbContext(options);
    }

    // ─── Test 1: Duplicate application rule ──────────────────────────────────

    [Fact]
    public async Task SubmitApplication_ShouldThrowValidationException_WhenSameEmailAppliesAgain()
    {
        // Arrange
        await using var db = CreateInMemoryDb();

        // Seed a job and a team member
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Backend Dev",
            Description = "...", Location = "Accra", Status = JobStatus.Open
        };
        db.Jobs.Add(job);

        // First application
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(), JobId = job.Id,
            CandidateName = "Kwame Boateng", CandidateEmail = "kwame@example.com",
            Stage = ApplicationStage.Applied, AppliedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // TODO: inject real NotificationService stub
        // var service = new ApplicationService(db, notificationServiceMock);

        // Act — same email, same job
        // var act = async () => await service.SubmitApplicationAsync(job.Id,
        //     new SubmitApplicationRequest("Kwame Boateng", "kwame@example.com", null));

        // Assert
        // await act.Should().ThrowAsync<ValidationException>()
        //     .WithMessage("*already applied*");

        // SCAFFOLD: test body written — uncomment when ApplicationService is implemented
        await Task.CompletedTask;
    }

    // ─── Test 2: Note author name is resolved correctly ───────────────────────

    [Fact]
    public async Task AddNote_ThenListNotes_ShouldReturnAuthorNameNotId()
    {
        // Arrange
        await using var db = CreateInMemoryDb();

        var teamMember = new TeamMember
        {
            Id = Guid.NewGuid(), Name = "Alice Mensah",
            Email = "alice@aihrly.com", Role = TeamMemberRole.Recruiter
        };
        var job = new Job
        {
            Id = Guid.NewGuid(), Title = "Dev", Description = "...",
            Location = "Accra", Status = JobStatus.Open
        };
        var application = new Application
        {
            Id = Guid.NewGuid(), JobId = job.Id,
            CandidateName = "Jane Doe", CandidateEmail = "jane@example.com",
            Stage = ApplicationStage.Applied, AppliedAt = DateTime.UtcNow
        };

        db.TeamMembers.Add(teamMember);
        db.Jobs.Add(job);
        db.Applications.Add(application);
        await db.SaveChangesAsync();

        // TODO: var noteService = new NoteService(db);
        // var addRequest = new AddNoteRequest("general", "Great candidate!");

        // Act
        // await noteService.AddNoteAsync(application.Id, teamMember.Id, addRequest);
        // var notes = await noteService.ListNotesAsync(application.Id);

        // Assert
        // notes.Should().HaveCount(1);
        // notes[0].AuthorName.Should().Be("Alice Mensah"); // name, not ID
        // notes[0].Type.Should().Be("General");

        // SCAFFOLD: uncomment when NoteService is implemented
        await Task.CompletedTask;
    }
}
