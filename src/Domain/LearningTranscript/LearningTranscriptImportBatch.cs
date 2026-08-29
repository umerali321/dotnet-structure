using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.LearningTranscript;

/// <summary>One row per import run of a Skillport "Asset Activity by User" style report. Written
/// directly by sp_ImportLearningTranscriptBatch, not through this class's (nonexistent) factory -
/// it exists as an EF entity purely so LearningTranscriptActivity can carry a real FK to it and so
/// the import history can be listed/read via normal LINQ.</summary>
public class LearningTranscriptImportBatch : IAggregateRoot
{
    public int ImportBatchId { get; private set; }

    public string SourceFileName { get; private set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; private set; }

    public string ImportedBy { get; private set; } = string.Empty;

    public int TotalRows { get; private set; }

    public int MatchedCount { get; private set; }

    public int UnmatchedCount { get; private set; }

    private LearningTranscriptImportBatch()
    {
    }
}
