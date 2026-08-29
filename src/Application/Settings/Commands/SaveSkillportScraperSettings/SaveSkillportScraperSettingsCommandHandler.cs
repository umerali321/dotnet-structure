using FluentValidation;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Skillsoft;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Settings.Commands.SaveSkillportScraperSettings;

public class SaveSkillportScraperSettingsCommandHandler
{
    private readonly IValidator<SaveSkillportScraperSettingsCommand> _validator;
    private readonly ISkillportScraperSettingsRepository _repository;

    public SaveSkillportScraperSettingsCommandHandler(
        IValidator<SaveSkillportScraperSettingsCommand> validator, ISkillportScraperSettingsRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task<SkillportScraperSettingsDto> Handle(
        SaveSkillportScraperSettingsCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can change Skillport scraper settings.");
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
