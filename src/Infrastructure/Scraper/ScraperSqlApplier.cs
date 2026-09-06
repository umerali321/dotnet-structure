using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Scraper.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Scraper;

/// <summary>Runs a scraper-generated SQL file (skillport_scraper.py's export_sql()) against the
/// database, batch-by-batch. The file uses "GO" as a batch separator the way SSMS/sqlcmd would -
/// that's not valid T-SQL for SqlCommand.ExecuteNonQuery, so it has to be split out here first.
/// Each course's own INSERT/UPDATE + CourseSections replace is one such batch (the script only
/// emits "GO" once per course, after all of that course's statements), so a bad batch is isolated
/// to that one course rather than rolling back or blocking the rest of the file.</summary>
public partial class ScraperSqlApplier : IScraperSqlApplier
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ScraperSqlApplier> _logger;

    public ScraperSqlApplier(ApplicationDbContext dbContext, ILogger<ScraperSqlApplier> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();

    public async Task<ScraperSqlApplyResult> ApplyAsync(string sqlFilePath, CancellationToken cancellationToken = default)
    {
        var script = await File.ReadAllTextAsync(sqlFilePath, cancellationToken);
        var batches = GoSeparator().Split(script).Where(b => !string.IsNullOrWhiteSpace(b)).ToList();

        var connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var succeeded = 0;
        var failed = 0;
        string? firstError = null;
        try
        {
            foreach (var batch in batches)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = batch;
                command.CommandTimeout = 120;
                try
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    succeeded++;
                }
                catch (Exception ex)
                {
                    failed++;
                    firstError ??= ex.Message;
                    _logger.LogError(ex, "Course scraper SQL batch failed (file: {SqlFilePath})", sqlFilePath);
                }
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }

        return new ScraperSqlApplyResult(batches.Count, succeeded, failed, firstError);
    }
}
