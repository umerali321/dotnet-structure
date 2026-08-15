using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.CourseLibrary;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class LibraryCategoryConfiguration : IEntityTypeConfiguration<LibraryCategory>
{
    public void Configure(EntityTypeBuilder<LibraryCategory> builder)
    {
        builder.ToTable("LibraryCategories");
        builder.HasKey(x => x.CategoryId);

        builder.Property(x => x.CategoryId).HasColumnName("CategoryID").ValueGeneratedOnAdd();
        builder.Property(x => x.TypeId).HasColumnName("TypeID");
        builder.Property(x => x.CategoryName).HasColumnName("CategoryName").HasMaxLength(255);
        builder.Property(x => x.CategoryUrl).HasColumnName("CategoryURL").HasMaxLength(2000);
        builder.Property(x => x.DisplayOrder).HasColumnName("DisplayOrder");
        builder.Property(x => x.IsActive).HasColumnName("IsActive");
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
    }
}
