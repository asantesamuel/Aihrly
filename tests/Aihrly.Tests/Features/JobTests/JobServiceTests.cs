using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Aihrly.Api.Features.Jobs;
using Aihrly.Api.Infrastructure.Persistence;
using Aihrly.Api.Common.Errors;

namespace Aihrly.Tests.Features.JobTests;

public class JobServiceTests
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

    private static JobService CreateService(AihrlyDbContext db) => new(db);

    // -----------------------------------------------------------------------
    // CreateJobAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateJob_ShouldPersistToDatabase_AndReturnResponse()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var request = new CreateJobRequest(
            Title: "Backend Developer",
            Description: "Build and maintain APIs",
            Location: "Accra, Ghana");

        var result = await service.CreateJobAsync(request);

        result.Title.Should().Be("Backend Developer");
        result.Description.Should().Be("Build and maintain APIs");
        result.Location.Should().Be("Accra, Ghana");
        result.Status.Should().Be("Open");
        result.Id.Should().NotBe(Guid.Empty);

        var inDb = await db.Jobs.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Title.Should().Be("Backend Developer");
    }

    [Fact]
    public async Task CreateJob_ShouldAlwaysStartWithOpenStatus()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.CreateJobAsync(
            new CreateJobRequest("Dev", "Desc", "Lagos"));

        result.Status.Should().Be("Open");
    }

    // -----------------------------------------------------------------------
    // ListJobsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ListJobs_WithNoFilter_ShouldReturnAllJobs()
    {
        await using var db = CreateDb();

        db.Jobs.AddRange(
            new Job { Id = Guid.NewGuid(), Title = "Dev A", Description = "D", Location = "L", Status = JobStatus.Open },
            new Job { Id = Guid.NewGuid(), Title = "Dev B", Description = "D", Location = "L", Status = JobStatus.Closed },
            new Job { Id = Guid.NewGuid(), Title = "Dev C", Description = "D", Location = "L", Status = JobStatus.Open }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListJobsAsync(new ListJobsQuery(null, 1, 20));

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListJobs_WithStatusFilter_ShouldReturnOnlyMatchingJobs()
    {
        await using var db = CreateDb();

        db.Jobs.AddRange(
            new Job { Id = Guid.NewGuid(), Title = "Open Job", Description = "D", Location = "L", Status = JobStatus.Open },
            new Job { Id = Guid.NewGuid(), Title = "Closed Job", Description = "D", Location = "L", Status = JobStatus.Closed }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListJobsAsync(new ListJobsQuery("open", 1, 20));

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Open Job");
        result.Items[0].Status.Should().Be("Open");
    }

    [Fact]
    public async Task ListJobs_Pagination_ShouldReturnCorrectPage()
    {
        await using var db = CreateDb();

        for (int i = 1; i <= 5; i++)
        {
            db.Jobs.Add(new Job
            {
                Id = Guid.NewGuid(),
                Title = $"Job {i:D2}",
                Description = "D",
                Location = "L",
                Status = JobStatus.Open
            });
        }
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListJobsAsync(new ListJobsQuery(null, 2, 2));

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items[0].Title.Should().Be("Job 03");
        result.Items[1].Title.Should().Be("Job 04");
    }

    [Fact]
    public async Task ListJobs_WithInvalidStatusFilter_ShouldReturnAllJobs()
    {
        await using var db = CreateDb();

        db.Jobs.Add(new Job
        {
            Id = Guid.NewGuid(),
            Title = "J",
            Description = "D",
            Location = "L",
            Status = JobStatus.Open
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListJobsAsync(new ListJobsQuery("banana", 1, 20));

        result.TotalCount.Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // GetJobByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetJobById_ShouldReturnJob_WhenItExists()
    {
        await using var db = CreateDb();

        var jobId = Guid.NewGuid();
        db.Jobs.Add(new Job
        {
            Id = jobId,
            Title = "Senior Dev",
            Description = "Lead the backend",
            Location = "Kumasi",
            Status = JobStatus.Open
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetJobByIdAsync(jobId);

        result.Id.Should().Be(jobId);
        result.Title.Should().Be("Senior Dev");
    }

    [Fact]
    public async Task GetJobById_ShouldThrowNotFoundException_WhenJobDoesNotExist()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var act = async () => await service.GetJobByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }
}