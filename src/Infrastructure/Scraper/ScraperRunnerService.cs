using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Scraper.Interfaces;
using SkillsetsBackend.Application.Scraper.Models;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Scraper;

/// <summary>Singleton, in-memory job tracker for the single Skillport scraper run that can ever be
/// in flight at once (only one Selenium/Chrome session against Skillport makes sense anyway). A
/// plain lock guards every read/write of the mutable state - writes are a few log lines a second
/// at most, reads are one poll every few seconds, so contention is a non-issue.
///
/// Accepted limitation: state is in-memory only. An API restart mid-run orphans the actual
/// Python/Selenium/Chrome process (it keeps running, unmonitored) while the reported status
/// resets to Idle. Fine for a single-admin, run-occasionally tool - not building PID/lock-file
/// recovery for this.</summary>
public class ScraperRunnerService : IScraperRunnerService
{
    private const int MaxLogLines = 1000;

    private static readonly Regex SqlFileLineRegex = new(@"^\[SQL\] Exported SQL to: (?<path>.+)$", RegexOptions.Compiled);

    private readonly SkillsoftScraperSettings _settings;
    private readonly ILogger<ScraperRunnerService> _logger;
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
    private Process? _currentProcess;

    public ScraperRunnerService(IOptions<SkillsoftScraperSettings> settings, ILogger<ScraperRunnerService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
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

            var snapshot = BuildSnapshot();
            _ = Task.Run(() => RunProcessAsync(categories, mode, limit));
            return snapshot;
        }
    }

    /// <summary>Force-kills the whole process tree (python + chromedriver + chrome) in one shot -
    /// this is actually more reliable than manually ending python.exe in Task Manager, which
    /// doesn't necessarily take its child chromedriver/chrome processes down with it and can
    /// leave zombies behind.</summary>
    public ScraperRunSnapshot StopRun()
    {
        Process? processToKill;
        lock (_lock)
        {
            if (_status != ScraperRunStatus.Running)
            {
                throw new ConflictException("No scraper run is currently in progress.");
            }
            processToKill = _currentProcess;
        }

        if (processToKill is not null)
        {
            try
            {
                if (!processToKill.HasExited)
                {
                    processToKill.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort - it may already be gone.
            }
        }

        lock (_lock)
        {
            // Only downgrade if RunProcessAsync's own completion handler hasn't already run and
            // recorded a more specific outcome for this same kill.
            if (_status == ScraperRunStatus.Running)
            {
                _status = ScraperRunStatus.Failed;
                _errorMessage = "Stopped by user.";
                _finishedAt = DateTimeOffset.UtcNow;
            }
            return BuildSnapshot();
        }
    }

    private async Task RunProcessAsync(IReadOnlyList<string> categories, string mode, int? limit)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _settings.PythonExecutablePath,
                WorkingDirectory = _settings.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add(_settings.ScriptPath);
            // One --category argument per selected category (not comma-joined) - some real
            // category names contain commas themselves (e.g. "CUSTOMER SERVICE, SALES &
            // MARKETING COURSES"), so a single delimited string would be ambiguous to split back
            // apart. Repeated arguments have no such collision.
            foreach (var category in categories)
            {
                psi.ArgumentList.Add("--category");
                psi.ArgumentList.Add(category);
            }
            psi.ArgumentList.Add("--mode");
            psi.ArgumentList.Add(mode);
            if (limit is > 0)
            {
                psi.ArgumentList.Add("--limit");
                psi.ArgumentList.Add(limit.Value.ToString());
            }
            // Always headless when triggered from the app - no one is watching a browser window
            // on a server.
            psi.ArgumentList.Add("--headless");
            psi.Environment["SKILLPORT_USER"] = _settings.SkillportUsername;
            psi.Environment["SKILLPORT_PASS"] = _settings.SkillportPassword;

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            try
            {
                process.OutputDataReceived += (_, e) => AppendLogLine(e.Data);
                process.ErrorDataReceived += (_, e) => AppendLogLine(e.Data);

                process.Start();
                lock (_lock)
                {
                    _currentProcess = process;
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(_settings.TimeoutMinutes));
                try
                {
                    await process.WaitForExitAsync(timeoutCts.Token);
                    // Per Microsoft's own documented guidance for Process + redirected async
                    // output: when output is read via BeginOutputReadLine()/OutputDataReceived,
                    // the exit-wait above can return before the last of those async callbacks
                    // have actually run - the process can be gone while a couple of its final
                    // stdout lines are still "in flight". Calling the parameterless WaitForExit()
                    // again forces a full drain of both redirected streams first. Without this,
                    // the very last log lines (like the "[SQL] Exported..." line the frontend
                    // keys off of) could still be unprocessed at the exact moment status flips to
                    // Completed - and since the frontend stops polling right when it sees
                    // Completed, it could get stuck showing a snapshot from just before the SQL
                    // path was recorded.
                    process.WaitForExit();
                }
                catch (OperationCanceledException)
                {
                    TryKill(process);
                    lock (_lock)
                    {
                        _status = ScraperRunStatus.Failed;
                        _errorMessage = $"Timed out after {_settings.TimeoutMinutes} minute(s).";
                        _finishedAt = DateTimeOffset.UtcNow;
                    }
                    return;
                }

                lock (_lock)
                {
                    // StopRun() may have already recorded "Stopped by user." for this same
                    // process exiting - don't clobber that with a generic exit-code message.
                    if (_status == ScraperRunStatus.Running)
                    {
                        _exitCode = process.ExitCode;
                        _status = process.ExitCode == 0 ? ScraperRunStatus.Completed : ScraperRunStatus.Failed;
                        _finishedAt = DateTimeOffset.UtcNow;
                        if (_status == ScraperRunStatus.Failed && _errorMessage is null)
                        {
                            _errorMessage = $"Scraper exited with code {process.ExitCode}.";
                        }
                    }
                }
            }
            finally
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_currentProcess, process))
                    {
                        _currentProcess = null;
                    }
                }
                process.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scraper run failed to start or crashed unexpectedly.");
            lock (_lock)
            {
                _status = ScraperRunStatus.Failed;
                _errorMessage = ex.Message;
                _finishedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private void AppendLogLine(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_lock)
        {
            _logLines.Add(line);
            if (_logLines.Count > MaxLogLines)
            {
                _logLines.RemoveAt(0);
            }

            var match = SqlFileLineRegex.Match(line);
            if (match.Success)
            {
                _sqlFilePath = match.Groups["path"].Value.Trim();
            }
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort - the process may have already exited on its own.
        }
    }

    private ScraperRunSnapshot BuildSnapshot() => new(
        _status, _category, _mode, _limit, _startedAt, _finishedAt, _startedByEmail,
        _logLines.ToList(), _errorMessage, _sqlFilePath, _exitCode);
}
