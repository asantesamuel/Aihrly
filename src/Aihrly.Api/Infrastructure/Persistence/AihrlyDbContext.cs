using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Infrastructure.Persistence;

/// <summary>
/// The main EF Core database context for Aihrly.
/// 
/// Responsibilities:
/// - Declares DbSets (one per entity = one per table)
/// - Applies entity configurations (indexes, constraints, relationships)
/// - Runs seed data at startup via HasData in configurations
/// </summary>
public class AihrlyDbContext : DbContext
{
    public AihrlyDbContext(DbContextOptions<AihrlyDbContext> options) : base(options) { }

    public DbSet<TeamMember>      TeamMembers      => Set<TeamMember>();
    public DbSet<Job>             Jobs             => Set<Job>();
    public DbSet<Application>     Applications     => Set<Application>();
    public DbSet<ApplicationNote> ApplicationNotes => Set<ApplicationNote>();
    public DbSet<StageHistory>    StageHistories   => Set<StageHistory>();
    public DbSet<ApplicationScore>ApplicationScores=> Set<ApplicationScore>();
    public DbSet<Notification>    Notifications    => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes from this assembly
        // This keeps OnModelCreating clean — each entity's config lives in its own file
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AihrlyDbContext).Assembly);
    }
}
