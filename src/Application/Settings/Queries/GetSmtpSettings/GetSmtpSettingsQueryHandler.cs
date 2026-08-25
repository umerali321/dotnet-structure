using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetSmtpSettings;

public class GetSmtpSettingsQueryHandler
{
    private readonly ISmtpSettingsRepository _repository;

    public GetSmtpSettingsQueryHandler(ISmtpSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SmtpSettingsDto?> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view SMTP settings.");
        }

        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            return null;
        }

        return new SmtpSettingsDto(
            settings.SmtpSettingsId, settings.Provider, settings.IsEnabled, settings.Host, settings.Port, settings.EnableSsl,
            settings.Username, !string.IsNullOrEmpty(settings.EncryptedPassword), settings.FromEmail, settings.FromName,
            settings.ReplyToEmail, settings.SupportToEmail, settings.SupportToName, settings.CreatedAt, settings.UpdatedAt);
    }
}
