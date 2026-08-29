using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.LearningTranscript;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class LearningTranscriptActivityConfiguration : IEntityTypeConfiguration<LearningTranscriptActivity>
{
    public void Configure(EntityTypeBuilder<LearningTranscriptActivity> builder)
    {
        builder.ToTable("LearningTranscriptActivities");
        builder.HasKey(x => x.LearningTranscriptActivityId);

        builder.Property(x => x.AssetId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CompletionStatus).HasMaxLength(50);

        builder.Property(x => x.AbsoluteHighScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.AbsoluteLastScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PreTestScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.HighScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CurrentScore).HasColumnType("decimal(5,2)");

        builder.Property(x => x.CreatedAt).IsRequired();

        // The single index the report's main query hits - exactly one "current" row per
        // (person, course) pair, so "give me the latest status" never needs to window/rank over
        // history at query time. sp_ImportLearningTranscriptBatch flips the old row's IsLatest to
        // 0 before inserting the new one, so this constraint is never actually contended.
        builder.HasIndex(x => new { x.LearningTranscriptIdentityId, x.AssetId })
            .IsUnique()
            .HasFilter("[IsLatest] = 1")
            .HasDatabaseName("IX_LearningTranscriptActivities_Identity_Asset_Latest");

        // Full history lookups for one person's one course, newest batch first.
        builder.HasIndex(x => new { x.LearningTranscriptIdentityId, x.AssetId, x.ImportBatchId })
            .HasDatabaseName("IX_LearningTranscriptActivities_Identity_Asset_History");

        builder.HasIndex(x => x.ImportBatchId);
    }
}
