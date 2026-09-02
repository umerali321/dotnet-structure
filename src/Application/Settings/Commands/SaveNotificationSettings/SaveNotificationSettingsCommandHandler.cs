using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Notifications;

namespace SkillsetsBackend.Application.Settings.Commands.SaveNotificationSettings;

public class SaveNotificationSettingsCommandHandler
{
    private readonly INotificationSettingsRepository _repository;
    private readonly IPermissionService _permissionService;

    public SaveNotificationSettingsCommandHandler(INotificationSettingsRepository repository,
        IPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    public async Task<NotificationSettingsDto> Handle(
        SaveNotificationSettingsCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven, not a hardcoded SuperAdmin check - this is what lets a SuperAdmin
        // hand a SystemAdmin exactly this screen and nothing else. SuperAdmin still passes:
        // IPermissionService returns true for them unconditionally.
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Settings.ManageNotifications, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to change notification settings.");
        }

        // Three booleans with no interdependence - nothing to validate beyond the type itself, so
        // there is deliberately no FluentValidation validator here.
        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = NotificationSettings.CreateDefault();
            _repository.Add(settings);
        }

        settings.Update(
            command.ReminderNotificationsEnabled,
            command.LoginNotificationsEnabled,
            command.AssignmentNotificationsEnabled);

        await _repository.SaveChangesAsync(cancellationToken);

        return new NotificationSettingsDto(
            settings.ReminderNotificationsEnabled,
            settings.LoginNotificationsEnabled,
            settings.AssignmentNotificationsEnabled,
            settings.UpdatedAt);
    }
}
