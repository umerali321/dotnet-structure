using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Skillsoft;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Settings.Commands.SaveSkillportScraperSettings;

public class SaveSkillportScraperSettingsCommandHandler
{
    private readonly IValidator<SaveSkillportScraperSettingsCommand> _validator;
    private readonly ISkillportScraperSettingsRepository _repository;
    private readonly IPermissionService _permissionService;

    public SaveSkillportScraperSettingsCommandHandler(
        IValidator<SaveSkillportScraperSettingsCommand> validator, ISkillportScraperSettingsRepository repository,
        IPermissionService permissionService)
    {
        _validator = validator;
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<SkillportScraperSettingsDto> Handle(
        SaveSkillportScraperSettingsCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageScraper, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to change the report scraper settings.");
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = SkillportScraperSettings.Create(command.GroupName);
            _repository.Add(settings);
        }
        else
        {
            settings.UpdateGroupName(command.GroupName);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return new SkillportScraperSettingsDto(
            settings.SkillportScraperSettingsId, settings.GroupName, settings.CreatedAt, settings.UpdatedAt);
    }
}
