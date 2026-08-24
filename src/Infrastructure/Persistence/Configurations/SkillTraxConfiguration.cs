using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class SkillTraxConfiguration : IEntityTypeConfiguration<SkillTrax>
{
    public void Configure(EntityTypeBuilder<SkillTrax> builder)
    {
        builder.ToTable("SkillTrax");
        builder.HasKey(x => x.SkillTraxId);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // Soft delete: DeleteSkillTraxCommandHandler only ever calls SkillTrax.Delete(), never a
        // real row removal - the global filter here guarantees every query (list, detail, get-by-id
        // for update/delete validation) automatically excludes deleted rows without needing an
        // explicit "not deleted" clause at every call site.
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Restrict, not Cascade - Users/Companies are never hard-deleted in this system (see
        // AGENTS.md), and Restrict avoids SQL Server's "multiple cascade paths" error since
        // AssignmentEmployee also cascades from a different root down to rows that ultimately
        // trace back to Users.
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompanyId);
    }
}
