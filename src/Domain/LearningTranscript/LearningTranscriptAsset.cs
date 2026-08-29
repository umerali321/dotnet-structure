using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.LearningTranscript;

/// <summary>Course/asset dimension for imported transcript data - one row per distinct Skillsoft
/// asset code, deduping the title/type/sub-type text the source report repeats on every activity
/// row. Populated/merged by sp_ImportLearningTranscriptBatch, read via normal LINQ elsewhere.
/// AssetId is Skillsoft's own asset code (e.g. "bs_ald17_a03_enus"), not a surrogate key - it is
/// already globally unique and is exactly what activity rows and Courses.SkillsoftCourseCode join
/// on, so introducing a separate identity column would only add an indirection nothing needs.</summary>
public class LearningTranscriptAsset : IAggregateRoot
{
    public string AssetId { get; private set; } = string.Empty;

    public string AssetTitle { get; private set; } = string.Empty;

    public string? AssetType { get; private set; }

    public string? AssetSubType { get; private set; }

    /// <summary>Resolved via Courses.SkillsoftCourseCode when it matches - optional enrichment
    /// link into the existing scraped course catalog. Null when this asset isn't in our catalog
    /// (e.g. it was retired there, or never scraped).</summary>
    public long? InternalCourseId { get; private set; }

    public DateTimeOffset FirstSeenAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    private LearningTranscriptAsset()
    {
    }
}
