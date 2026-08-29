using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.LearningTranscript;

/// <summary>One row per import batch per (Identity, Asset) pair - append-only, so full history is
/// preserved across imports rather than overwritten in place. IsLatest marks the single current
/// row for a given (LearningTranscriptIdentityId, AssetId) pair (enforced by a unique filtered
/// index in LearningTranscriptActivityConfiguration) so the report's main query never has to
/// window/rank over history just to show "current status" - it just filters IsLatest = 1. Written
/// exclusively by sp_ImportLearningTranscriptBatch, which flips the previous latest row's IsLatest
/// to 0 before inserting the new one for the same pair.
///
/// "Absolute*" fields are the source report's all-time (ever) figures, distinct from the plain
/// fields which reflect the current enrollment cycle - both are kept since they carry different
/// meaning, not duplicated the way the raw export mechanically repeats each column. Durations are
/// stored as plain minutes rather than a SQL time value since cumulative "Absolute" durations can
/// exceed 24 hours.</summary>
public class LearningTranscriptActivity : IAggregateRoot
{
    public long LearningTranscriptActivityId { get; private set; }

    public int ImportBatchId { get; private set; }

    public int LearningTranscriptIdentityId { get; private set; }

    public string AssetId { get; private set; } = string.Empty;

    public bool IsLatest { get; private set; }

    public int? TimesRestarted { get; private set; }

    public DateOnly? AbsoluteFirstAccessDate { get; private set; }

    public DateOnly? AbsoluteLastAccessDate { get; private set; }

    public int? AbsoluteTimesAccessed { get; private set; }

    public decimal? AbsoluteHighScore { get; private set; }

    public decimal? AbsoluteLastScore { get; private set; }

    public int? AbsoluteActualDurationMinutes { get; private set; }

    public DateOnly? FirstAccessDate { get; private set; }

    public DateOnly? LastAccessDate { get; private set; }

    public int? TimesAccessed { get; private set; }

    public int? TimesDownloaded { get; private set; }

    public DateOnly? DownloadDate { get; private set; }

    public int? HtmlPageReads { get; private set; }

    public DateOnly? EnrollmentDate { get; private set; }

    public DateOnly? CompletionDate { get; private set; }

    public string? CompletionStatus { get; private set; }

    public decimal? PreTestScore { get; private set; }

    public int? MaxTestAttempts { get; private set; }

    public int? ActualTestAttempts { get; private set; }

    public decimal? HighScore { get; private set; }

    public decimal? CurrentScore { get; private set; }

    public int? ExpectedDurationMinutes { get; private set; }

    public int? ActualDurationMinutes { get; private set; }

    public DateOnly? LastSkillportLoginDate { get; private set; }

    public DateOnly? SkillportRegistrationDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private LearningTranscriptActivity()
    {
    }
}
