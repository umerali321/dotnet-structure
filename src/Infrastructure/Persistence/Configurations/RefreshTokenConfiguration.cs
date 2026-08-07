using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(x => x.Token).IsUnique();

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.CompanyName).HasMaxLength(255);
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.RevokedByIp).HasMaxLength(64);
        builder.Property(x => x.ReplacedByToken).HasMaxLength(512);

        builder.HasIndex(x => x.UserId);
    }
}
