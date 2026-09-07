using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Scraper.Interfaces;
using SkillsetsBackend.Application.Scraper.Models;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Scraper;

/// <summary>Singleton, in-memory job tracker for the single Skillport scraper run that can ever be
/// in flight at once (only one Selenium/Chrome session against Skillport makes sense anyway). A
/// plain lock guards every read/write of the mutable state - writes are a few log lines a second
/// at most, reads are one poll every few seconds, so contention is a non-issue.
///
/// Runs the scraper via its own on-demand Windows Scheduled Task ("SkillSets - Course Library
/// Scraper", registered by register_course_scraper_task.ps1) rather than launching python.exe
/// directly - launching it directly as a different account (ProcessStartInfo.UserName/Password)
/// does not fully replicate a normal login: confirmed live, Python's own _ssl module failed a DLL
/// initialization that way even though the exact same script/account works fine both run by hand
/// and run via Task Scheduler. Since Task Scheduler gives no way to stream a triggered task's
/// stdout back to the caller, categories/mode/limit are handed off via a small params file the
/// script reads (--params-file) and progress is tracked by tailing a log file the script itself
/// writes, polling Task Scheduler's own status in between.
///
/// Accepted limitation: state is in-memory only. An API restart mid-run orphans the actual
/// Scheduled Task run (it keeps going, unmonitored) while the reported status resets to Idle.
/// Fine for a single-admin, run-occasionally tool - not building persisted-state recovery for
/// this.</summary>
public class ScraperRunnerService : IScraperRunnerService
{
    private const int MaxLogLines = 1000;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    private static readonly Regex SqlFileLineRegex = new(@"^\[SQL\] Exported SQL to: (?<path>.+)$", RegexOptions.Compiled);

    private readonly SkillsoftScraperSettings _settings;
    private readonly ILogger<ScraperRunnerService> _logger;
    // IScraperSqlApplier/IScraperTaskRunner are Scoped, but this service is a Singleton - a scope is
    // created on demand around each call that needs one, rather than injecting them directly, which
    // would capture their dependencies (a DbContext, in the applier's case) for the app's entire
    // lifetime.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _lock = new();

    private ScraperRunStatus _status = ScraperRunStatus.Idle;
    private string? _category;
    private string? _mode;
    private int? _limit;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _finishedAt;
    private string? _startedByEmail;
    private readonly List<string> _logLines = new();
    private string? _errorMessage;
    private string? _sqlFilePath;
    private int? _exitCode;
    private bool _sqlApplied;
    private int? _sqlBatchesSucceeded;
    private int? _sqlBatchesFailed;
    private string? _sqlApplyError;
    // Cancels the current run's poll loop from StopRunAsync - replaces a directly-owned Process
    // handle now that the actual work happens inside a Scheduled Task this service doesn't own a
    // handle to.
    private CancellationTokenSource? _runCts;

    public ScraperRunnerService(
        IOptions<SkillsoftScraperSettings> settings,
        ILogger<ScraperRunnerService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public ScraperRunSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return BuildSnapshot();
        }
    }

    public ScraperRunSnapshot StartRun(IReadOnlyList<string> categories, string mode, int? limit, string startedByEmail)
    {
        lock (_lock)
        {
            if (_status == ScraperRunStatus.Running)
            {
                throw new ConflictException("A scraper run is already in progress.");
            }

            _status = ScraperRunStatus.Running;
            _category = string.Join(", ", categories);
            _mode = mode;
            _limit = limit;
            _startedAt = DateTimeOffset.UtcNow;
            _finishedAt = null;
            _startedByEmail = startedByEmail;
            _logLines.Clear();
            _errorMessage = null;
            _sqlFilePath = null;
            _exitCode = null;
            _sqlApplied = false;
            _sqlBatchesSucceeded = null;
            _sqlBatchesFailed = null;
            _sqlApplyError = null;

            _runCts = new CancellationTokenSource();
            var runCts = _runCts;

            var snapshot = BuildSnapshot();
            _ = Task.Run(() => RunAsync(categories, mode, limit, runCts));
            return snapshot;
        }
    }

    /// <summary>Ends the currently-running Scheduled Task instance (schtasks /End, which takes the
    /// whole python + chromedriver + chrome process tree with it) and cancels this service's own
    /// poll loop for it.</summary>
    public async Task<ScraperRunSnapshot> StopRunAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? runCts;
        lock (_lock)
        {
            if (_status != ScraperRunStatus.Running)
            {
                throw new ConflictException("No scraper run is currently in progress.");
            }
            runCts = _runCts;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var taskRunner = scope.ServiceProvider.GetRequiredService<IScraperTaskRunner>();
            await taskRunner.EndAsync(ScraperTaskNames.CourseLibraryScraper, cancellationToken);
        }
        catch
        {
            // Best-effort - the task may have already finished on its own.
        }

        runCts?.Cancel();

        lock (_lock)
        {
            // Only downgrade if RunAsync's own completion handler hasn't already run and recorded
            // a more specific outcome for this same stop.
            if (_status == ScraperRunStatus.Running)
            {
                _status = ScraperRunStatus.Failed;
                _errorMessage = "Stopped by user.";
                _finishedAt = DateTimeOffset.UtcNow;
            }
            return BuildSnapshot();
        }
    }

    private async Task RunAsync(IReadOnlyList<string> categories, string mode, int? limit, CancellationTokenSource runCts)
    {
        try
        {
            var paramsFilePath = Path.Combine(_settings.WorkingDirectory, "course_scraper_params.json");
            var logFilePath = Path.Combine(_settings.WorkingDirectory, "course_scraper_run.log");

            var paramsPayload = JsonSerializer.Serialize(new
            {
                categories = categories.Count > 0 ? categories : null,
                mode,
                limit,
            });
            await File.WriteAllTextAsync(paramsFilePath, paramsPayload, runCts.Token);

            using var scope = _scopeFactory.CreateScope();
            var taskRunner = scope.ServiceProvider.GetRequiredService<IScraperTaskRunner>();

            var triggerResult = await taskRunner.TriggerNowAsync(ScraperTaskNames.CourseLibraryScraper, runCts.Token);
            if (!triggerResult.Started)
            {
                lock (_lock)
                {
                    _status = ScraperRunStatus.Failed;
                    _errorMessage = triggerResult.ErrorMessage ?? "Failed to start the Course Library scraper task.";
                    _finishedAt = DateTimeOffset.UtcNow;
                }
                return;
            }

            var deadline = DateTimeOffset.UtcNow.AddMinutes(_settings.TimeoutMinutes);
            // Task Scheduler needs a moment to actually transition the task to Running after /Run
            // returns - only treating "seen Running, then not Running" as "finished" avoids reading
            // that brief gap as an already-completed run.
            var sawRunning = false;
            string? lastResult = null;

            while (true)
            {
                await Task.Delay(PollInterval, runCts.Token);
                TailLogFile(logFilePath);

                var taskStatus = await taskRunner.GetStatusAsync(ScraperTaskNames.CourseLibraryScraper, runCts.Token);
                var isRunning = string.Equals(taskStatus.Status, "Running", StringComparison.OrdinalIgnoreCase);
                if (isRunning)
                {
                    sawRunning = true;
                }
                else if (sawRunning)
                {
                    lastResult = taskStatus.LastResult;
                    break;
                }

                if (DateTimeOffset.UtcNow > deadline)
                {
                    await taskRunner.EndAsync(ScraperTaskNames.CourseLibraryScraper, CancellationToken.None);
                    lock (_lock)
                    {
                        _status = ScraperRunStatus.Failed;
                        _errorMessage = $"Timed out after {_settings.TimeoutMinutes} minute(s).";
                        _finishedAt = DateTimeOffset.UtcNow;
                    }
                    return;
                }
            }

            // One last tail - the script may have written its final lines in the moment between the
            // last poll and Task Scheduler reporting the run as no longer active.
            TailLogFile(logFilePath);

            lock (_lock)
            {
                if (_status == ScraperRunStatus.Running)
                {
                    var succeeded = lastResult == "0";
                    _status = succeeded ? ScraperRunStatus.Completed : ScraperRunStatus.Failed;
                    _finishedAt = DateTimeOffset.UtcNow;
                    _exitCode = int.TryParse(lastResult, out var code) ? code : null;
                    if (_status == ScraperRunStatus.Failed && _errorMessage is null)
                    {
                        _errorMessage = $"Scraper task's last result was {lastResult ?? "unknown"}.";
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // StopRunAsync already recorded "Stopped by user." for this same run.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Course Library scraper run failed to start or crashed unexpectedly.");
            lock (_lock)
            {
                _status = ScraperRunStatus.Failed;
                _errorMessage = ex.Message;
                _finishedAt = DateTimeOffset.UtcNow;
            }
        }

        string? sqlFilePathToApply;
        lock (_lock)
        {
            // Not gated on Completed: skillport_scraper.py exports SQL per-category as each one
            // finishes, so a run that later crashes (e.g. a Chrome/Selenium disconnect deep into an
            // ALL-categories run) can still have produced a real, usable SQL file covering whatever
            // categories DID finish before the crash - applying it recovers that work instead of
            // discarding potentially hours of successful scraping over one late failure.
            sqlFilePathToApply = _sqlFilePath is not null && File.Exists(_sqlFilePath)
                ? _sqlFilePath
                : null;
        }

        // Applying the generated SQL is what actually gets scraped courses into the portal - a
        // successful scrape that just leaves a file on disk for someone to remember to run by hand
        // is exactly the gap that caused courses to go missing in the first place.
        if (sqlFilePathToApply is not null)
        {
            await ApplySqlFileAsync(sqlFilePathToApply);
        }
    }

    private void TailLogFile(string logFilePath)
    {
        string content;
        try
        {
            if (!File.Exists(logFilePath))
            {
                return;
            }

            // Shared read access - the script may have the file open for its next append (or a
            // fresh truncate at the very start of the next run) at the same moment this reads it.
            using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            content = reader.ReadToEnd();
        }
        catch (IOException)
        {
            // Being written to at the exact moment of this poll - pick it up on the next tick.
            return;
        }

        lock (_lock)
        {
            _logLines.Clear();
            foreach (var rawLine in content.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }
                _logLines.Add(line);

                var match = SqlFileLineRegex.Match(line);
                if (match.Success)
                {
                    // skillport_scraper.py's OUT_DIR is relative to its own working directory (the
                    // Scheduled Task's configured WorkingDirectory), not to this API process's own
                    // - resolve against the same WorkingDirectory so File.Exists/File.ReadAllBytes
                    // elsewhere don't silently miss it.
                    var reportedPath = match.Groups["path"].Value.Trim();
                    _sqlFilePath = Path.IsPathRooted(reportedPath)
                        ? reportedPath
                        : Path.Combine(_settings.WorkingDirectory, reportedPath);
                }
            }

            if (_logLines.Count > MaxLogLines)
            {
                _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);
            }
        }
    }

    private async Task ApplySqlFileAsync(string sqlFilePath)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var applier = scope.ServiceProvider.GetRequiredService<IScraperSqlApplier>();
            var result = await applier.ApplyAsync(sqlFilePath);

            lock (_lock)
            {
                _sqlApplied = true;
                _sqlBatchesSucceeded = result.BatchesSucceeded;
                _sqlBatchesFailed = result.BatchesFailed;
                _sqlApplyError = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply scraper SQL file '{SqlFilePath}' to the database.", sqlFilePath);
            lock (_lock)
            {
                _sqlApplied = false;
                _sqlApplyError = ex.Message;
            }
        }
    }

    private ScraperRunSnapshot BuildSnapshot() => new(
        _status, _category, _mode, _limit, _startedAt, _finishedAt, _startedByEmail,
        _logLines.ToList(), _errorMessage, _sqlFilePath, _exitCode,
        _sqlApplied, _sqlBatchesSucceeded, _sqlBatchesFailed, _sqlApplyError);
}
