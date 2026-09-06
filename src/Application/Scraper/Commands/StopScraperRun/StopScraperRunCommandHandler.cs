using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Scraper.DTOs;
using SkillsetsBackend.Application.Scraper.Interfaces;

namespace SkillsetsBackend.Application.Scraper.Commands.StopScraperRun;

public class StopScraperRunCommandHandler
{
    private readonly IScraperRunnerService _runner;

    public StopScraperRunCommandHandler(IScraperRunnerService runner)
    {
        _runner = runner;
    }

    public async Task<ScraperRunStatusDto> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can stop the course scraper.");
        }

        var snapshot = await _runner.StopRunAsync(cancellationToken);
        return ScraperRunStatusDto.FromSnapshot(snapshot);
    }
}
