using SkillsetsBackend.Application.Scraper.Models;

namespace SkillsetsBackend.Application.Scraper.DTOs;

public record ScraperRunStatusDto(
    string Status,
    string? Category,
    string? Mode,
    int? Limit,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? StartedByEmail,
    IReadOnlyList<string> LogTail,
    string? ErrorMessage,
    bool SqlFileAvailable,
    string? SqlFileName)
{
    public static ScraperRunStatusDto FromSnapshot(ScraperRunSnapshot snapshot) => new(
        snapshot.Status.ToString(),
        snapshot.Category,
        snapshot.Mode,
        snapshot.Limit,
        snapshot.StartedAt,
        snapshot.FinishedAt,
        snapshot.StartedByEmail,
        snapshot.LogTail,
        snapshot.ErrorMessage,
        snapshot.SqlFilePath is not null,
        snapshot.SqlFilePath is not null ? Path.GetFileName(snapshot.SqlFilePath) : null);
}
