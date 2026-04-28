using Aihrly.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aihrly.Api.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.Description)
            .IsRequired();

        builder.Property(j => j.Location)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>(); // stored as "Open" / "Closed"

        // Index on Status — GET /api/jobs?status=open filters by this column frequently
        builder.HasIndex(j => j.Status);

        // One job has many applications
        builder.HasMany(j => j.Applications)
            .WithOne(a => a.Job)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict); // don't cascade-delete applications if job is deleted
    }
}
