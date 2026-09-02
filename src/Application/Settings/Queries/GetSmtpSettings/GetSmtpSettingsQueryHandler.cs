using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetSmtpSettings;

public class GetSmtpSettingsQueryHandler
{
    private readonly ISmtpSettingsRepository _repository;
    private readonly IPermissionService _permissionService;

    public GetSmtpSettingsQueryHandler(ISmtpSettingsRepository repository,
        IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<SmtpSettingsDto?> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageEmail, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view SMTP settings.");
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
