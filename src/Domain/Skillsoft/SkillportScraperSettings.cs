using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Skillsoft;

/// <summary>Singleton row (there is only ever at most one) holding the SuperAdmin-managed Skillport
/// group that asset_activity_report_scraper.py targets - the scraper reads this value directly from
/// the database at startup instead of a hardcoded constant, so a SuperAdmin can point it at a
/// different company's Skillport group without touching the script.</summary>
public class SkillportScraperSettings : IAggregateRoot
{
    public int SkillportScraperSettingsId { get; private set; }

    /// <summary>The Skillport "Group Org Code" or group title the scraper searches for and selects
    /// in the Asset Activity by User report's Groups/Users filter (e.g. "LC_17").</summary>
    public string GroupName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private SkillportScraperSettings()
    {
    }

    public static SkillportScraperSettings Create(string groupName)
    {
        return new SkillportScraperSettings
        {
            GroupName = groupName,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateGroupName(string groupName)
    {
        GroupName = groupName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
