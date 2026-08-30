using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Settings.DTOs;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Application.Settings.Queries.GetNotificationSettings;

public class GetNotificationSettingsQueryHandler
{
    private readonly INotificationSettingsRepository _repository;

    public GetNotificationSettingsQueryHandler(INotificationSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotificationSettingsDto> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view notification settings.");
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
