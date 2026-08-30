using SkillsetsBackend.Domain.Notifications;

namespace SkillsetsBackend.Application.Settings.Interfaces;

/// <summary>There is at most one NotificationSettings row - a genuine singleton, not a list a caller
/// filters down. GetAsync returns null until a SuperAdmin saves the screen for the first time, which
/// callers must read as "everything enabled": these emails were already being sent before the switches
/// existed, so an absent row must not silently turn them off.</summary>
public interface INotificationSettingsRepository
{
    Task<NotificationSettings?> GetAsync(CancellationToken cancellationToken = default);

    void Add(NotificationSettings settings);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
