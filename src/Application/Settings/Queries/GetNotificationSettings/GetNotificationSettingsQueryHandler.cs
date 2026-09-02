using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetNotificationSettings;

public class GetNotificationSettingsQueryHandler
{
    private readonly INotificationSettingsRepository _repository;
    private readonly IPermissionService _permissionService;

    public GetNotificationSettingsQueryHandler(INotificationSettingsRepository repository,
        IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<NotificationSettingsDto> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageNotifications, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view notification settings.");
        }

        var settings = await _repository.GetAsync(cancellationToken);

        // No row yet means nothing has been turned off - report everything enabled, which is what the
        // dispatcher itself does, so the screen matches actual behaviour rather than showing every
        // switch off before the SuperAdmin has ever opened it.
        return settings is null
            ? new NotificationSettingsDto(true, true, true, null)
            : new NotificationSettingsDto(
                settings.ReminderNotificationsEnabled,
                settings.LoginNotificationsEnabled,
                settings.AssignmentNotificationsEnabled,
                settings.UpdatedAt);
    }
}
