using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

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
    }
}
