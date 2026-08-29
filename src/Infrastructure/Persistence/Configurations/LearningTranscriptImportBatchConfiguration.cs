using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.LearningTranscript;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class LearningTranscriptImportBatchConfiguration : IEntityTypeConfiguration<LearningTranscriptImportBatch>
{
    public void Configure(EntityTypeBuilder<LearningTranscriptImportBatch> builder)
    {
        builder.ToTable("LearningTranscriptImportBatches");
        builder.HasKey(x => x.ImportBatchId);

        builder.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ImportedBy).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ImportedAt).IsRequired();
        builder.Property(x => x.TotalRows).IsRequired();
        builder.Property(x => x.MatchedCount).IsRequired();
        builder.Property(x => x.UnmatchedCount).IsRequired();
    }
}
