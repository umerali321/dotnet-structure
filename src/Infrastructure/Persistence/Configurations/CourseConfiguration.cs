using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(x => x.CourseId);

        builder.Property(x => x.CourseId).HasColumnName("CourseID").ValueGeneratedOnAdd();
        builder.Property(x => x.CategoryId).HasColumnName("CategoryID");
        builder.Property(x => x.SubCategoryId).HasColumnName("SubCategoryID");
        builder.Property(x => x.CourseTitle).HasColumnName("CourseTitle").HasMaxLength(1000);
        builder.Property(x => x.CourseUrl).HasColumnName("CourseURL").HasMaxLength(2000);
        builder.Property(x => x.LaunchUrl).HasColumnName("LaunchURL");
        builder.Property(x => x.Duration).HasColumnName("Duration").HasMaxLength(100);
        builder.Property(x => x.ExpertiseLevel).HasColumnName("ExpertiseLevel").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("Status").HasMaxLength(255);
        builder.Property(x => x.AboutContent).HasColumnName("AboutContent");
        builder.Property(x => x.OverviewContent).HasColumnName("OverviewContent");
        builder.Property(x => x.ImageUrl).HasColumnName("ImageURL").HasMaxLength(2000);
        builder.Property(x => x.SkillsoftCourseCode).HasColumnName("SkillsoftCourseCode").HasMaxLength(500);
        builder.Property(x => x.DisplayOrder).HasColumnName("DisplayOrder");
        builder.Property(x => x.IsActive).HasColumnName("IsActive");
        builder.Property(x => x.ScrapedAt).HasColumnName("ScrapedAt").HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.CourseSourceUrl).HasColumnName("CourseSourceUrl").HasMaxLength(1000);
        builder.Property(x => x.SourceStatus).HasColumnName("SourceStatus").HasMaxLength(100);

        builder.HasIndex(x => x.CategoryId);
    }
}
