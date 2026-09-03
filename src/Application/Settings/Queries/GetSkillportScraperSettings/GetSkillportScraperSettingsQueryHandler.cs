using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetSkillportScraperSettings;

public class GetSkillportScraperSettingsQueryHandler
{
    private readonly ISkillportScraperSettingsRepository _repository;
    private readonly IPermissionService _permissionService;

    public GetSkillportScraperSettingsQueryHandler(ISkillportScraperSettingsRepository repository,
        IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<SkillportScraperSettingsDto?> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageScraper, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view the report scraper settings.");
        }

        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            return null;
        }

        return new SkillportScraperSettingsDto(
            settings.SkillportScraperSettingsId, settings.GroupName, settings.DateRangeMode,
            settings.CustomDateFrom, settings.CustomDateTo, settings.CreatedAt, settings.UpdatedAt);
    }
}
