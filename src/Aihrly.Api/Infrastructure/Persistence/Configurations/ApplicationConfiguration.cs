using Aihrly.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aihrly.Api.Infrastructure.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CandidateName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.CandidateEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(a => a.Stage)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(a => a.AppliedAt)
            .IsRequired();

        // UNIQUE CONSTRAINT: same candidate cannot apply to same job twice
        // This enforces the business rule at the database level — a safety net
        // even if the service-layer check is bypassed somehow
        builder.HasIndex(a => new { a.JobId, a.CandidateEmail })
            .IsUnique()
            .HasDatabaseName("IX_Applications_JobId_CandidateEmail");

        // Index on JobId — GET /api/jobs/{jobId}/applications queries this frequently
        builder.HasIndex(a => a.JobId);

        // Index on Stage — GET /api/jobs/{jobId}/applications?stage=screening
        builder.HasIndex(a => a.Stage);

        builder.HasMany(a => a.Notes)
            .WithOne(n => n.Application)
            .HasForeignKey(n => n.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.StageHistory)
            .WithOne(sh => sh.Application)
            .HasForeignKey(sh => sh.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Scores)
            .WithOne(s => s.Application)
            .HasForeignKey(s => s.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Notifications)
            .WithOne(n => n.Application)
            .HasForeignKey(n => n.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
