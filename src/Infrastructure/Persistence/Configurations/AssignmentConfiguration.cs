using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(x => x.AssignmentId);

        builder.Property(x => x.SourceType).HasConversion<int>().IsRequired();

        // Restrict, not Cascade - see SkillTraxConfiguration for why (Users/Companies are never
        // hard-deleted; Restrict avoids SQL Server's multi-cascade-path error).
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        // Deleting a SkillTrax must never erase historical assignment data (blueprint requirement)
        // - AssignmentTitles already holds the real course snapshot independent of this reference,
        // so nulling it out here on delete is safe; it only affects the "assigned from this
        // SkillTrax" provenance label, never the assignment's actual content.
        builder.HasOne<SkillTrax>().WithMany().HasForeignKey(x => x.SourceSkillTraxId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.SourceSkillTraxId);
    }
}
