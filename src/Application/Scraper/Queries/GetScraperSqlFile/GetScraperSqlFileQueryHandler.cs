using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Scraper.Interfaces;

namespace SkillsetsBackend.Application.Scraper.Queries.GetScraperSqlFile;

public record ScraperSqlFileResult(byte[] Content, string FileName);

public class GetScraperSqlFileQueryHandler
{
    private readonly IScraperRunnerService _runner;

    public GetScraperSqlFileQueryHandler(IScraperRunnerService runner)
    {
        _runner = runner;
    }

    public async Task<ScraperSqlFileResult> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can download the course scraper's SQL file.");
        }

        var snapshot = _runner.GetSnapshot();
        if (snapshot.SqlFilePath is null || !File.Exists(snapshot.SqlFilePath))
        {
            throw new NotFoundException("Scraper SQL file", "current run");
        }

        var content = await File.ReadAllBytesAsync(snapshot.SqlFilePath, cancellationToken);
        return new ScraperSqlFileResult(content, Path.GetFileName(snapshot.SqlFilePath));
    }
}
