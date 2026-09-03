using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(100);
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.Username).HasMaxLength(320);
        builder.Property(x => x.PasswordHash).HasMaxLength(500);

        // Existing rows predate this column; the migration backfills them to "Legacy" and the
        // default keeps any insert path that doesn't set it explicitly honest rather than blank.
        builder.Property(x => x.CreationSource)
            .HasMaxLength(CreationSource.MaxLength)
            .IsRequired()
            .HasDefaultValue(CreationSource.Manual);

        // Existing columns are datetime2 (no offset) - see DateTimeOffsetToDateTime2Converter.
        builder.Property(x => x.CreatedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.UpdatedAt).HasConversion(NullableDateTimeOffsetToDateTime2Converter.Instance);

        // Matches IX_Users_Email/IX_Users_FirstName_LastName - a prefix search on Phone (Employees
        // screen's "Search by phone") needs its own seekable index for the same reason those do: a
        // LIKE 'term%' scan against 162k+ Users rows with no index is measured at hundreds of ms.
        builder.HasIndex(x => x.Phone).HasDatabaseName("IX_Users_Phone");
    }
}
