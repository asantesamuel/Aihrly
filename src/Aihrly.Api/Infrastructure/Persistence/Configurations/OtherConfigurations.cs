using Aihrly.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aihrly.Api.Infrastructure.Persistence.Configurations;

public class ApplicationNoteConfiguration : IEntityTypeConfiguration<ApplicationNote>
{
    public void Configure(EntityTypeBuilder<ApplicationNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(n => n.Description)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Index on ApplicationId — GET /api/applications/{id}/notes queries this
        builder.HasIndex(n => n.ApplicationId);

        builder.HasOne(n => n.CreatedBy)
            .WithMany(tm => tm.Notes)
            .HasForeignKey(n => n.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StageHistoryConfiguration : IEntityTypeConfiguration<StageHistory>
{
    public void Configure(EntityTypeBuilder<StageHistory> builder)
    {
        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.FromStage)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sh => sh.ToStage)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sh => sh.ChangedAt)
            .IsRequired();

        // Index on ApplicationId — GET /api/applications/{id} loads full stage history
        builder.HasIndex(sh => sh.ApplicationId);

        builder.HasOne(sh => sh.ChangedBy)
            .WithMany(tm => tm.StageChanges)
            .HasForeignKey(sh => sh.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ApplicationScoreConfiguration : IEntityTypeConfiguration<ApplicationScore>
{
    public void Configure(EntityTypeBuilder<ApplicationScore> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Dimension)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.Score)
            .IsRequired();

        builder.Property(s => s.ScoredAt)
            .IsRequired();

        // UNIQUE CONSTRAINT: one row per (Application, Dimension)
        // PUT semantics = upsert on this composite key
        builder.HasIndex(s => new { s.ApplicationId, s.Dimension })
            .IsUnique()
            .HasDatabaseName("IX_ApplicationScores_ApplicationId_Dimension");

        builder.HasOne(s => s.ScoredBy)
            .WithMany(tm => tm.Scores)
            .HasForeignKey(s => s.ScoredById)
            .OnDelete(DeleteBehavior.Restrict);

        // UpdatedBy is nullable (only set on second submission)
        builder.HasOne(s => s.UpdatedBy)
            .WithMany()
            .HasForeignKey(s => s.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(n => n.SentAt)
            .IsRequired();

        builder.HasIndex(n => n.ApplicationId);
    }
}
