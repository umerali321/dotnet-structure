using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Scraper.DTOs;
using SkillsetsBackend.Application.Scraper.Interfaces;

namespace SkillsetsBackend.Application.Scraper.Queries.GetScraperRunStatus;

public class GetScraperRunStatusQueryHandler
{
    private readonly IScraperRunnerService _runner;

    public GetScraperRunStatusQueryHandler(IScraperRunnerService runner)
    {
        _runner = runner;
    }

    public Task<ScraperRunStatusDto> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view the course scraper status.");
        }

        return Task.FromResult(ScraperRunStatusDto.FromSnapshot(_runner.GetSnapshot()));
    }
}
