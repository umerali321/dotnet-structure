using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class CourseSectionConfiguration : IEntityTypeConfiguration<CourseSection>
{
    public void Configure(EntityTypeBuilder<CourseSection> builder)
    {
        builder.ToTable("CourseSections");
        builder.HasKey(x => x.SectionId);

        builder.Property(x => x.SectionId).HasColumnName("SectionID").ValueGeneratedOnAdd();
        builder.Property(x => x.CourseId).HasColumnName("CourseID");
        builder.Property(x => x.SectionName).HasColumnName("SectionName").HasMaxLength(1000);
        builder.Property(x => x.SectionUrl).HasColumnName("SectionURL").HasMaxLength(2000);
        builder.Property(x => x.Duration).HasColumnName("Duration").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("Status").HasMaxLength(255);
        builder.Property(x => x.DisplayOrder).HasColumnName("DisplayOrder");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasConversion(DateTimeOffsetToDateTime2Converter.Instance);

        builder.HasIndex(x => x.CourseId);
    }
}
