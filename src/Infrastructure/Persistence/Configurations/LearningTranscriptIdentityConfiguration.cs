using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.LearningTranscript;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class LearningTranscriptIdentityConfiguration : IEntityTypeConfiguration<LearningTranscriptIdentity>
{
    public void Configure(EntityTypeBuilder<LearningTranscriptIdentity> builder)
    {
        builder.ToTable("LearningTranscriptIdentities");
        builder.HasKey(x => x.LearningTranscriptIdentityId);

        builder.Property(x => x.SkillportUsername).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.SkillportUsername).IsUnique();

        // Null = unmatched, surfaced in the reconciliation view instead of the main report - not
        // a real FK constraint, since a match may legitimately not exist yet.
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.DisplayFirstName).HasMaxLength(100);
        builder.Property(x => x.DisplayLastName).HasMaxLength(100);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.UserStatus).HasMaxLength(50);
        builder.Property(x => x.GroupName).HasMaxLength(200);
        builder.Property(x => x.GroupOrgCode).HasMaxLength(100);
        builder.Property(x => x.GroupPath).HasMaxLength(500);
        builder.Property(x => x.ApprovalManagerId).HasMaxLength(100);
        builder.Property(x => x.ApprovalManagerFirstName).HasMaxLength(100);
        builder.Property(x => x.ApprovalManagerLastName).HasMaxLength(100);
        builder.Property(x => x.ResolutionMethod).HasMaxLength(50);

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
