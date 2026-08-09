using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class SupportContactConfiguration : IEntityTypeConfiguration<SupportContact>
{
    public void Configure(EntityTypeBuilder<SupportContact> builder)
    {
        builder.ToTable("SupportContacts");
        builder.HasKey(x => x.SupportContactId);

        builder.Property(x => x.ContactType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(320);
        builder.Property(x => x.UpdatedBy).HasMaxLength(320);

        // Brand-new table created by our own migration - EF's default datetimeoffset convention
        // applies, unlike the legacy datetime2 columns elsewhere (see AppUserConfiguration).
        builder.HasIndex(x => x.CompanyId);
    }
}
