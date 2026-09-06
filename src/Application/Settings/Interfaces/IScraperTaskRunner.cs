namespace SkillsetsBackend.Application.Settings.Interfaces;

/// <summary>Started = true if Windows accepted the run request (does not wait for the scrape itself
/// to finish - that can take several minutes). ErrorMessage carries schtasks.exe's own stderr/stdout
/// text when Started is false, so a failure in production can be diagnosed from the API response
/// instead of guessing - schtasks reports specific, human-readable reasons (e.g. "Access is denied",
/// "The specified account name is not valid") that would otherwise be silently discarded.</summary>
public record ScraperTaskRunResult(bool Started, string? ErrorMessage);

/// <summary>Raw values from schtasks /Query - kept as the strings Windows itself reports (e.g.
/// Status "Running"/"Ready", Last Result "0" for success) rather than parsed into enums/DateTimes,
/// since these are locale-formatted display text meant for an admin to read, not to compute on.
/// ErrorMessage is set instead of the rest when the query itself couldn't be answered.</summary>
public record ScraperTaskStatus(string? Status, string? LastRunTime, string? LastResult, string? NextRunTime, string? ErrorMessage);

/// <summary>Triggers the nightly Learning Transcript scraper's Windows Scheduled Task immediately,
/// instead of waiting for its normal midnight schedule - lets an admin change the Group/Date Range
/// in Settings and see it take effect right away, without needing server (RDP) access.</summary>
public interface IScraperTaskRunner
{
    Task<ScraperTaskRunResult> TriggerNowAsync(CancellationToken cancellationToken = default);

    /// <summary>Lets an admin see whether the task is currently running and how its last run went,
    /// from the same Settings screen - without this, Run Now only ever confirms the trigger was
    /// accepted, never whether the scrape itself finished or succeeded.</summary>
    Task<ScraperTaskStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}
