using SkillsetsBackend.Domain.Communications;

namespace SkillsetsBackend.Application.Settings.Interfaces;

/// <summary>There is at most one SmtpSettings row - a genuine singleton, not a list a caller filters
/// down. GetAsync returns null until a SuperAdmin saves a configuration for the first time.</summary>
public interface ISmtpSettingsRepository
{
    Task<SmtpSettings?> GetAsync(CancellationToken cancellationToken = default);

    void Add(SmtpSettings settings);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
