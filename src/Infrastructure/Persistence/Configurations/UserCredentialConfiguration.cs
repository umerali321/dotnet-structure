using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class UserCredentialConfiguration : IEntityTypeConfiguration<UserCredential>
{
    public void Configure(EntityTypeBuilder<UserCredential> builder)
    {
        builder.ToTable("UserCredentials");
        builder.HasKey(x => x.UserCredentialId);

        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.UserId);
    }
}
