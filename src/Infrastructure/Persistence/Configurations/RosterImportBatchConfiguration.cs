using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.RosterImport;
using SkillsetsBackend.Infrastructure.Persistence.Conversions;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class RosterImportBatchConfiguration : IEntityTypeConfiguration<RosterImportBatch>
{
    public void Configure(EntityTypeBuilder<RosterImportBatch> builder)
    {
        builder.ToTable("RosterImportBatches");
        builder.HasKey(x => x.RosterImportBatchId);

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ImportedByEmail).HasMaxLength(320).IsRequired();

        // See AppUserConfiguration - this database stores datetime2, not datetimeoffset.
        builder.Property(x => x.ImportedAt).HasConversion(DateTimeOffsetToDateTime2Converter.Instance);
        builder.Property(x => x.WelcomeEmailsSentAt).HasConversion(NullableDateTimeOffsetToDateTime2Converter.Instance);

        builder.HasMany(x => x.Rows)
            .WithOne()
            .HasForeignKey(x => x.RosterImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // The "my recent imports" list is always newest-first.
        builder.HasIndex(x => x.ImportedAt).HasDatabaseName("IX_RosterImportBatches_ImportedAt");

        builder.Navigation(x => x.Rows).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class RosterImportBatchRowConfiguration : IEntityTypeConfiguration<RosterImportBatchRow>
{
    public void Configure(EntityTypeBuilder<RosterImportBatchRow> builder)
    {
        builder.ToTable("RosterImportBatchRows");
        builder.HasKey(x => x.RosterImportBatchRowId);

        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.EmployeeType).HasMaxLength(20);
        builder.Property(x => x.Status).HasMaxLength(RosterImportRowStatus.MaxLength).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        // The welcome-email step selects exactly "rows of this batch that created a user", so the
        // filtered index matches that query rather than scanning every row of the batch.
        builder.HasIndex(x => new { x.RosterImportBatchId, x.UserId })
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_RosterImportBatchRows_Batch_User");
    }
}
