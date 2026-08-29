using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetSkillportScraperSettings;

public class GetSkillportScraperSettingsQueryHandler
{
    private readonly ISkillportScraperSettingsRepository _repository;

    public GetSkillportScraperSettingsQueryHandler(ISkillportScraperSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SkillportScraperSettingsDto?> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view Skillport scraper settings.");
        }

        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            return null;
        }

        return new SkillportScraperSettingsDto(
            settings.SkillportScraperSettingsId, settings.GroupName, settings.CreatedAt, settings.UpdatedAt);
    }
}
