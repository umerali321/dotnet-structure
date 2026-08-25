using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Communications;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class SmtpSettingsConfiguration : IEntityTypeConfiguration<SmtpSettings>
{
    public void Configure(EntityTypeBuilder<SmtpSettings> builder)
    {
        builder.ToTable("SmtpSettings");
        builder.HasKey(x => x.SmtpSettingsId);

        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(255).IsRequired();
        builder.Property(x => x.EncryptedPassword).HasMaxLength(2000);
        builder.Property(x => x.FromEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FromName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ReplyToEmail).HasMaxLength(255);
        builder.Property(x => x.SupportToEmail).HasMaxLength(255);
        builder.Property(x => x.SupportToName).HasMaxLength(255);

        builder.Property(x => x.CreatedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.UpdatedAt).HasConversion(NullableDateTimeOffsetToDateTime2Converter.Instance);
    }
}
