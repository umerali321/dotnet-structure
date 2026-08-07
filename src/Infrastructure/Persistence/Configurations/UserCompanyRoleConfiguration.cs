using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

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
    }
}
