using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(x => x.CompanyId);

        builder.Property(x => x.CompanyCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CompanyEmail).HasMaxLength(255);
        builder.Property(x => x.CompanyPhone).HasMaxLength(100);
    }
}
