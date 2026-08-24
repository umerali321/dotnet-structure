using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Domain.CourseLibrary;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class SkillTraxCourseConfiguration : IEntityTypeConfiguration<SkillTraxCourse>
{
    public void Configure(EntityTypeBuilder<SkillTraxCourse> builder)
    {
        builder.ToTable("SkillTraxCourses");
        builder.HasKey(x => new { x.SkillTraxId, x.CourseId });

        builder.HasOne<SkillTrax>().WithMany().HasForeignKey(x => x.SkillTraxId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
    }
}
