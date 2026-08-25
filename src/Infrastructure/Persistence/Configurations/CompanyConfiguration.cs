using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

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
        builder.Property(x => x.Street1).HasMaxLength(255);
        builder.Property(x => x.Street2).HasMaxLength(255);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.Zip).HasMaxLength(20);
        builder.Property(x => x.PaymentForm).HasMaxLength(100);
        builder.Property(x => x.TotalPayment).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LogoUrl).HasMaxLength(500);

        // Defaults below only matter for backfilling companies that existed before this column was
        // added (Company.Create always sets explicit values) - a far-future PlanEndDate ensures no
        // pre-existing company is treated as expired once the login/list expiry check goes live.
        builder.Property(x => x.PlanType).HasMaxLength(20).HasDefaultValue(Company.LicensePlan);
        builder.Property(x => x.PlanStartDate).HasDefaultValue(new DateOnly(2020, 1, 1));
        builder.Property(x => x.PlanEndDate).HasDefaultValue(new DateOnly(2099, 12, 31));
        builder.Property(x => x.PurchaseDate);
        builder.Ignore(x => x.IsExpired);

        // See AppUserConfiguration - existing columns are datetime2, not datetimeoffset.
        builder.Property(x => x.CreatedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.UpdatedAt).HasConversion(NullableDateTimeOffsetToDateTime2Converter.Instance);
    }
}
