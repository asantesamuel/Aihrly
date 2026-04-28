using Aihrly.Api.Domain.Entities;
using Aihrly.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aihrly.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the TeamMember table.
/// 
/// Seed data: 3 team members are pre-populated at startup.
/// These are the only actors in the system — their IDs are documented
/// in the README so anyone running the API knows what to put in
/// the X-Team-Member-Id header.
/// </summary>
public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    // Fixed GUIDs so migrations are idempotent (same ID every time)
    public static readonly Guid AliceId  = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid BobId    = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid CarolId  = Guid.Parse("00000000-0000-0000-0000-000000000003");

    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Email)
            .IsRequired()
            .HasMaxLength(320); // max valid email length

        builder.Property(t => t.Role)
            .IsRequired()
            .HasConversion<string>(); // store as "Recruiter" / "HiringManager" in DB

        // Unique constraint — no two team members with the same email
        builder.HasIndex(t => t.Email).IsUnique();

        // Seed data — 2 recruiters, 1 hiring manager
        builder.HasData(
            new TeamMember
            {
                Id    = AliceId,
                Name  = "Alice Mensah",
                Email = "alice@aihrly.com",
                Role  = TeamMemberRole.Recruiter
            },
            new TeamMember
            {
                Id    = BobId,
                Name  = "Bob Asante",
                Email = "bob@aihrly.com",
                Role  = TeamMemberRole.Recruiter
            },
            new TeamMember
            {
                Id    = CarolId,
                Name  = "Carol Owusu",
                Email = "carol@aihrly.com",
                Role  = TeamMemberRole.HiringManager
            }
        );
    }
}
