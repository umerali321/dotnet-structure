using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Skillsoft;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class SkillportScraperSettingsConfiguration : IEntityTypeConfiguration<SkillportScraperSettings>
{
    public void Configure(EntityTypeBuilder<SkillportScraperSettings> builder)
    {
        builder.ToTable("SkillportScraperSettings");
        builder.HasKey(x => x.SkillportScraperSettingsId);

        builder.Property(x => x.GroupName).HasMaxLength(200).IsRequired();

        builder.Property(x => x.CreatedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.UpdatedAt).HasConversion(NullableDateTimeOffsetToDateTime2Converter.Instance);
    }
}
