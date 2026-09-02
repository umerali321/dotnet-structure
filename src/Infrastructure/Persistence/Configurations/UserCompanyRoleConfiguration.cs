using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class UserCompanyRoleConfiguration : IEntityTypeConfiguration<UserCompanyRole>
{
    public void Configure(EntityTypeBuilder<UserCompanyRole> builder)
    {
        builder.ToTable("UserCompanyRoles");
        builder.HasKey(x => x.UserCompanyRoleId);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.CreationSource)
            .HasMaxLength(CreationSource.MaxLength)
            .IsRequired()
            .HasDefaultValue(CreationSource.Manual);

        // Answers "how many employees/managers were created manually vs by roster import" directly
        // off the index, without scanning UserCompanyRoles - the reason this column exists.
        builder.HasIndex(x => new { x.CreationSource, x.RoleId })
            .HasDatabaseName("IX_UserCompanyRoles_CreationSource_Role");

        // See AppUserConfiguration - existing column is datetime2, not datetimeoffset.
        builder.Property(x => x.CreatedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
    }
}
