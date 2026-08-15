using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class LoginActivityLogConfiguration : IEntityTypeConfiguration<LoginActivityLog>
{
    public void Configure(EntityTypeBuilder<LoginActivityLog> builder)
    {
        builder.ToTable("LoginActivityLogs");
        builder.HasKey(x => x.LoginActivityLogId);

        builder.Property(x => x.EventType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.Message).HasMaxLength(2000);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.EventType);
    }
}
