namespace SkillsetsBackend.Application.Settings.DTOs;

/// <summary>Master switches for the automated notification emails. A switch that is off stops that
/// notification being sent at all - see NotificationDispatcher, which is the single place they are
/// enforced.</summary>
public record NotificationSettingsDto(
    bool ReminderNotificationsEnabled,
    bool LoginNotificationsEnabled,
    bool AssignmentNotificationsEnabled,
    DateTimeOffset? UpdatedAt);
