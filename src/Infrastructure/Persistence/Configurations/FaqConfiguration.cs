using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("Faqs");
        builder.HasKey(x => x.FaqId);

        builder.Property(x => x.Question).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Answer).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(320);
        builder.Property(x => x.UpdatedBy).HasMaxLength(320);

        // Brand-new table created by our own migration - EF's default datetimeoffset convention
        // applies, unlike the legacy datetime2 columns elsewhere (see AppUserConfiguration).
        builder.HasIndex(x => x.CompanyId);
    }
}
