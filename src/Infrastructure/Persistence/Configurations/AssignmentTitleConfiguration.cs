using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Domain.CourseLibrary;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class AssignmentTitleConfiguration : IEntityTypeConfiguration<AssignmentTitle>
{
    public void Configure(EntityTypeBuilder<AssignmentTitle> builder)
    {
        builder.ToTable("AssignmentTitles");
        builder.HasKey(x => new { x.AssignmentId, x.CourseId });

        builder.HasOne<Assignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
    }
}
