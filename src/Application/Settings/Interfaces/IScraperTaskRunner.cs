namespace SkillsetsBackend.Application.Settings.Interfaces;

/// <summary>Started = true if Windows accepted the run request (does not wait for the scrape itself
/// to finish - that can take several minutes). ErrorMessage carries schtasks.exe's own stderr/stdout
/// text when Started is false, so a failure in production can be diagnosed from the API response
/// instead of guessing - schtasks reports specific, human-readable reasons (e.g. "Access is denied",
/// "The specified account name is not valid") that would otherwise be silently discarded.</summary>
public record ScraperTaskRunResult(bool Started, string? ErrorMessage);

/// <summary>Triggers the nightly Learning Transcript scraper's Windows Scheduled Task immediately,
/// instead of waiting for its normal midnight schedule - lets an admin change the Group/Date Range
/// in Settings and see it take effect right away, without needing server (RDP) access.</summary>
public interface IScraperTaskRunner
{
    Task<ScraperTaskRunResult> TriggerNowAsync(CancellationToken cancellationToken = default);
}
