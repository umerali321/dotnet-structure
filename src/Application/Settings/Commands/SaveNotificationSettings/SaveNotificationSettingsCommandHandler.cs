using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Domain.Notifications;

namespace SkillsetsBackend.Application.Settings.Commands.SaveNotificationSettings;

public class SaveNotificationSettingsCommandHandler
{
    private readonly INotificationSettingsRepository _repository;

    public SaveNotificationSettingsCommandHandler(INotificationSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotificationSettingsDto> Handle(
        SaveNotificationSettingsCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can change notification settings.");
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
