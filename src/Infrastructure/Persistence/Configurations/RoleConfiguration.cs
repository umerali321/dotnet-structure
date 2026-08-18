using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.RoleId);

        builder.Property(x => x.RoleName).HasMaxLength(50).IsRequired();

        // New, additive column - see the AddRolePermissionsAndManagerAssignment migration, which
        // also sets this true for the 5 pre-existing rows (Student/Manager/FDM/Admin/CompanyAdmin).
        builder.Property(x => x.IsSystemRole).HasDefaultValue(false);
    }
}
