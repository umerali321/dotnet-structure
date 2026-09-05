namespace SkillsetsBackend.Application.Settings.Interfaces;

/// <summary>Triggers the nightly Learning Transcript scraper's Windows Scheduled Task immediately,
/// instead of waiting for its normal midnight schedule - lets an admin change the Group/Date Range
/// in Settings and see it take effect right away, without needing server (RDP) access.</summary>
public interface IScraperTaskRunner
{
    /// <summary>Returns true if the task was successfully told to start. This does not wait for the
    /// scrape itself to finish (that can take several minutes) - it only confirms Windows accepted
    /// the run request, the same way Start-ScheduledTask does.</summary>
    Task<bool> TriggerNowAsync(CancellationToken cancellationToken = default);
}
