using SkillsetsBackend.Domain.Skillsoft;

namespace SkillsetsBackend.Application.Settings.Interfaces;

/// <summary>There is at most one SkillportScraperSettings row - a genuine singleton, not a list a
/// caller filters down. GetAsync returns null until a SuperAdmin saves a group name for the first
/// time (the scraper falls back to its own hardcoded default in that case).</summary>
public interface ISkillportScraperSettingsRepository
{
    Task<SkillportScraperSettings?> GetAsync(CancellationToken cancellationToken = default);

    void Add(SkillportScraperSettings settings);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
