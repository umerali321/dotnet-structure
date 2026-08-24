namespace SkillsetsBackend.Application.Scraper.Models;

public enum ScraperRunStatus
{
    Idle,
    Running,
    Completed,
    Failed,
}

/// <summary>Immutable point-in-time copy of the scraper's current/last run - the mutable working
/// state (log buffer, etc.) inside IScraperRunnerService never leaves the service itself.</summary>
public record ScraperRunSnapshot(
    ScraperRunStatus Status,
    string? Category,
    string? Mode,
    int? Limit,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? StartedByEmail,
    IReadOnlyList<string> LogTail,
    string? ErrorMessage,
    string? SqlFilePath,
    int? ExitCode);
