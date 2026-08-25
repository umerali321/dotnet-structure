using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Communications;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Settings.Commands.SaveSmtpSettings;

public class SaveSmtpSettingsCommandHandler
{
    private readonly IValidator<SaveSmtpSettingsCommand> _validator;
    private readonly ISmtpSettingsRepository _repository;
    private readonly ISecretProtector _secretProtector;

    public SaveSmtpSettingsCommandHandler(
        IValidator<SaveSmtpSettingsCommand> validator, ISmtpSettingsRepository repository, ISecretProtector secretProtector)
    {
        _validator = validator;
        _repository = repository;
        _secretProtector = secretProtector;
    }

    public async Task<SmtpSettingsDto> Handle(SaveSmtpSettingsCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can change SMTP settings.");
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var settings = await _repository.GetAsync(cancellationToken);
        var hasPassword = !string.IsNullOrEmpty(settings?.EncryptedPassword);

        if (string.IsNullOrWhiteSpace(command.Password) && !hasPassword)
        {
            throw new AppValidationException([new ValidationFailure(nameof(command.Password), "A password is required the first time SMTP is configured.")]);
        }

        if (settings is null)
        {
            settings = SmtpSettings.Create(
                command.Provider, command.IsEnabled, command.Host, command.Port, command.EnableSsl, command.Username,
                command.FromEmail, command.FromName, command.ReplyToEmail, command.SupportToEmail, command.SupportToName);
            _repository.Add(settings);
        }
        else
        {
            settings.UpdateConfiguration(
                command.Provider, command.IsEnabled, command.Host, command.Port, command.EnableSsl, command.Username,
                command.FromEmail, command.FromName, command.ReplyToEmail, command.SupportToEmail, command.SupportToName);
        }

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            settings.SetEncryptedPassword(_secretProtector.Protect(command.Password));
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return new SmtpSettingsDto(
            settings.SmtpSettingsId, settings.Provider, settings.IsEnabled, settings.Host, settings.Port, settings.EnableSsl,
            settings.Username, !string.IsNullOrEmpty(settings.EncryptedPassword), settings.FromEmail, settings.FromName,
            settings.ReplyToEmail, settings.SupportToEmail, settings.SupportToName, settings.CreatedAt, settings.UpdatedAt);
    }
}
