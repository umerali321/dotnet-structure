namespace SkillsetsBackend.Application.Scraper.Interfaces;

/// <summary>Result of executing a scraper-generated SQL file against the database. BatchesFailed
/// counts individual per-course batches that threw (e.g. a constraint violation on one malformed
/// row) - the rest of the file still runs, since each course's INSERT/UPDATE + section replace is
/// self-contained (its own "GO"-delimited batch), so one bad course doesn't block the others.</summary>
public record ScraperSqlApplyResult(int BatchesApplied, int BatchesSucceeded, int BatchesFailed, string? ErrorMessage);

public interface IScraperSqlApplier
{
    Task<ScraperSqlApplyResult> ApplyAsync(string sqlFilePath, CancellationToken cancellationToken = default);
}
