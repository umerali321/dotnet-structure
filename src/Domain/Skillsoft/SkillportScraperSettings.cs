using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Skillsoft;

/// <summary>The four reporting windows daily_transcript_sync.py can scrape - kept as plain string
/// constants (not a C# enum) because the value is read by the Python side via a raw SQL SELECT,
/// where a string is what naturally round-trips without a shared type to keep in sync.</summary>
public static class SkillportScraperDateRangeModes
{
    public const string Today = "Today";
    public const string Month = "Month";
    public const string Year = "Year";
    public const string Custom = "Custom";

    public static readonly IReadOnlyCollection<string> All = [Today, Month, Year, Custom];
}

/// <summary>Singleton row (there is only ever at most one) holding the SuperAdmin-managed settings
/// asset_activity_report_scraper.py and daily_transcript_sync.py read directly from the database at
/// startup instead of hardcoded constants, so a SuperAdmin can retarget the scraper without touching
/// any script.</summary>
public class SkillportScraperSettings : IAggregateRoot
{
    public int SkillportScraperSettingsId { get; private set; }

    /// <summary>The Skillport "Group Org Code" or group title the scraper searches for and selects
    /// in the Asset Activity by User report's Groups/Users filter (e.g. "LC_17").</summary>
    public string GroupName { get; private set; } = string.Empty;

    /// <summary>Which reporting window the nightly scrape pulls - see SkillportScraperDateRangeModes.
    /// Defaults to Today: a job that runs every 24 hours re-scraping a full year on every single run
    /// was the reason this became configurable in the first place.</summary>
    public string DateRangeMode { get; private set; } = SkillportScraperDateRangeModes.Today;

    /// <summary>Only meaningful (and required) when DateRangeMode is Custom.</summary>
    public DateOnly? CustomDateFrom { get; private set; }

    public DateOnly? CustomDateTo { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private SkillportScraperSettings()
    {
    }

    public static SkillportScraperSettings Create(
        string groupName, string dateRangeMode, DateOnly? customDateFrom, DateOnly? customDateTo)
    {
        return new SkillportScraperSettings
        {
            GroupName = groupName,
            DateRangeMode = dateRangeMode,
            CustomDateFrom = customDateFrom,
            CustomDateTo = customDateTo,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateGroupName(string groupName)
    {
        GroupName = groupName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDateRange(string dateRangeMode, DateOnly? customDateFrom, DateOnly? customDateTo)
    {
        DateRangeMode = dateRangeMode;
        CustomDateFrom = customDateFrom;
        CustomDateTo = customDateTo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
