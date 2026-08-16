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

        // The real exclusivity guarantee (app-layer checks in the command handler are the fast
        // path / friendly error message; these indexes are what actually prevents a race from
        // producing two active rows for the same student or the same course).
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_CourseTakens_ActiveUser");

        builder.HasIndex(x => x.CourseId)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_CourseTakens_ActiveCourse");
    }
}
