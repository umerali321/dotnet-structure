using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Settings.Queries.GetSkillportScraperStatus;

public class GetSkillportScraperStatusQueryHandler
{
    private readonly IPermissionService _permissionService;
    private readonly IScraperTaskRunner _taskRunner;

    public GetSkillportScraperStatusQueryHandler(IPermissionService permissionService, IScraperTaskRunner taskRunner)
    {
        _permissionService = permissionService;
        _taskRunner = taskRunner;
    }

    public async Task<ScraperTaskStatus> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        // Same permission as Run Now/settings - reading the schedule's status is no more sensitive
        // than triggering it.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageScraper, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view the report scraper status.");
        }

        return await _taskRunner.GetStatusAsync(cancellationToken);
    }
}
