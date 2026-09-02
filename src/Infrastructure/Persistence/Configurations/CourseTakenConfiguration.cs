using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.CourseLibrary;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class CourseTakenConfiguration : IEntityTypeConfiguration<CourseTaken>
{
    public void Configure(EntityTypeBuilder<CourseTaken> builder)
    {
        builder.ToTable("CourseTakens");
        builder.HasKey(x => x.CourseTakenId);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CourseId).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        // NOT unique any more. This used to be a filtered UNIQUE index enforcing "one active course
        // per student", which was the real teeth behind that rule - removing only the handler check
        // would have left the database rejecting the second course with a raw constraint violation.
        //
        // Kept as a plain index because the lookup it serves (this student's active courses) is on
        // every Course Library page load; only the uniqueness is gone.
        builder.HasIndex(x => x.UserId)
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_CourseTakens_ActiveUser");
    }
}
