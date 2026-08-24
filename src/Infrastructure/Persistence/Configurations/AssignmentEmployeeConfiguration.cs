using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class AssignmentEmployeeConfiguration : IEntityTypeConfiguration<AssignmentEmployee>
{
    public void Configure(EntityTypeBuilder<AssignmentEmployee> builder)
    {
        builder.ToTable("AssignmentEmployees");
        builder.HasKey(x => new { x.AssignmentId, x.StudentUserId });

        builder.HasOne<Assignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.StudentUserId);
    }
}
