using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Settings.Commands.RunSkillportScraperNow;

public record RunSkillportScraperNowResultDto(bool Started);

public class RunSkillportScraperNowCommandHandler
{
    private readonly IPermissionService _permissionService;
    private readonly IScraperTaskRunner _taskRunner;

    public RunSkillportScraperNowCommandHandler(IPermissionService permissionService, IScraperTaskRunner taskRunner)
    {
        _permissionService = permissionService;
        _taskRunner = taskRunner;
    }

    public async Task<RunSkillportScraperNowResultDto> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        // Same permission as changing the settings this run uses (GroupName/DateRangeMode) -
        // whoever can configure what to scrape can also trigger it, permission-driven so a
        // SuperAdmin can hand a SystemAdmin exactly this screen and nothing else.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageScraper, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to run the report scraper.");
        }

        var started = await _taskRunner.TriggerNowAsync(cancellationToken);
        return new RunSkillportScraperNowResultDto(started);
    }
}
