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

        // The real exclusivity guarantee (the app-layer check in the command handler is just the
        // fast path / friendly error message) - one active course per student. Deliberately no
        // equivalent unique index on CourseId alone: a course must be startable by many students
        // across many companies at the same time (see CourseTaken's class doc for the bug this
        // used to cause when such a global per-course index existed).
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_CourseTakens_ActiveUser");
    }
}
