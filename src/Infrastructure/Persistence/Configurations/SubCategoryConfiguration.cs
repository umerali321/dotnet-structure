using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class SubCategoryConfiguration : IEntityTypeConfiguration<SubCategory>
{
    public void Configure(EntityTypeBuilder<SubCategory> builder)
    {
        builder.ToTable("SubCategories");
        builder.HasKey(x => x.SubCategoryId);

        builder.Property(x => x.SubCategoryId).HasColumnName("SubCategoryID").ValueGeneratedOnAdd();
        builder.Property(x => x.CategoryId).HasColumnName("CategoryID");
        builder.Property(x => x.SubCategoryName).HasColumnName("SubCategoryName").HasMaxLength(500);
        builder.Property(x => x.SubCategoryUrl).HasColumnName("SubCategoryURL").HasMaxLength(2000);
        builder.Property(x => x.DisplayOrder).HasColumnName("DisplayOrder");
        builder.Property(x => x.IsActive).HasColumnName("IsActive");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
    }
}
