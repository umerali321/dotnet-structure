namespace SkillsetsBackend.Application.Settings.Commands.SaveNotificationSettings;

public record SaveNotificationSettingsCommand(
    bool ReminderNotificationsEnabled,
    bool LoginNotificationsEnabled,
    bool AssignmentNotificationsEnabled);
