using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Skillsoft;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class SkillportSessionConfiguration : IEntityTypeConfiguration<SkillportSession>
{
    public void Configure(EntityTypeBuilder<SkillportSession> builder)
    {
        builder.ToTable("SkillportSessions");
        builder.HasKey(x => x.SkillportSessionId);

        builder.Property(x => x.SkillportUsername).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SkillportPassword).HasMaxLength(50).IsRequired();

        // Brand-new table created by our own migration - EF's default datetimeoffset convention
        // applies for CreatedAt/ActivatedAt, unlike the legacy datetime2 columns elsewhere.
        builder.HasIndex(x => new { x.UserId, x.CompanyId });
    }
}
