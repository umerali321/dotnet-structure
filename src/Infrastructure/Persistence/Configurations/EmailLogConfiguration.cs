using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Communications;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");
        builder.HasKey(x => x.EmailLogId);

        builder.Property(x => x.FromAddress).HasMaxLength(255);
        builder.Property(x => x.FromName).HasMaxLength(255);
        builder.Property(x => x.ToAddress).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ToName).HasMaxLength(255);
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BodyHtml).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Purpose).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.Property(x => x.SentAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);

        // Email History is paged and ordered by most-recent-first - this is the one query shape it exists for.
        builder.HasIndex(x => x.SentAt);
    }
}
