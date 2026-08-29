using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.LearningTranscript;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class LearningTranscriptAssetConfiguration : IEntityTypeConfiguration<LearningTranscriptAsset>
{
    public void Configure(EntityTypeBuilder<LearningTranscriptAsset> builder)
    {
        builder.ToTable("LearningTranscriptAssets");

        // Natural key - Skillsoft's own asset code (e.g. "bs_ald17_a03_enus"), already globally
        // unique and exactly what activity rows and Courses.SkillsoftCourseCode join on.
        builder.HasKey(x => x.AssetId);
        builder.Property(x => x.AssetId).HasMaxLength(200).ValueGeneratedNever();

        builder.Property(x => x.AssetTitle).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AssetType).HasMaxLength(100);
        builder.Property(x => x.AssetSubType).HasMaxLength(100);
        builder.Property(x => x.FirstSeenAt).IsRequired();
        builder.Property(x => x.LastSeenAt).IsRequired();

        builder.HasIndex(x => x.InternalCourseId);
    }
}
