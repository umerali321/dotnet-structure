using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles");
        builder.HasKey(x => x.StudentProfileId);

        builder.Property(x => x.StudentType).HasMaxLength(20);

        // New, additive, nullable columns - see the AddStudentProfileAuditColumns migration.
        // UpdatedAt was created as datetimeoffset (EF Core's default), but CreatedAt is the
        // original legacy column and is datetime2 - see AppUserConfiguration for why this matters.
        builder.Property(x => x.CreatedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.CreatedBy).HasMaxLength(320);
        builder.Property(x => x.UpdatedBy).HasMaxLength(320);

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
