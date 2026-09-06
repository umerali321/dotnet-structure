using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillsetsBackend.Application.Settings.Interfaces;
using SkillsetsBackend.Infrastructure.Options;

namespace SkillsetsBackend.Infrastructure.Settings;

/// <summary>Shells out to schtasks.exe rather than a Task Scheduler library - one command, no new
/// package, and it's the exact same mechanism an admin already uses manually
/// (Start-ScheduledTask/schtasks /Run) so behaviour matches what's already been tested in
/// production. Windows-only by construction: this app is only ever deployed to the Windows Server
/// that also runs the scheduled task itself, so there is no cross-platform concern here -
/// [SupportedOSPlatform] makes that explicit to the platform-compatibility analyzer (CA1416) rather
/// than leaving warnings on the Domain/PasswordInClearText/LoadUserProfile calls below.
///
/// Shared by both the nightly Learning Transcript sync and the on-demand Course Library scraper -
/// every method takes the target task's name rather than hardcoding one.</summary>
[SupportedOSPlatform("windows")]
public class WindowsScraperTaskRunner : IScraperTaskRunner
{
    private readonly ScraperTaskRunnerSettings _settings;
    private readonly ILogger<WindowsScraperTaskRunner> _logger;

    public WindowsScraperTaskRunner(IOptions<ScraperTaskRunnerSettings> settings, ILogger<WindowsScraperTaskRunner> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ScraperTaskRunResult> TriggerNowAsync(string taskName, CancellationToken cancellationToken = default)
    {
        // The IIS application pool this API runs under normally has no permission to trigger a task
        // it doesn't own (e.g. one registered under the Administrator account) - Task Scheduler's
        // own security descriptor blocks it, and editing that descriptor by hand is fragile. Running
        // schtasks.exe AS that owning account instead sidesteps the ACL question entirely. Falls
        // back to running as the app pool's own identity when unset (e.g. local dev, where this
        // task doesn't exist anyway).
        var usingConfiguredCredentials = HasConfiguredCredentials;
        var startInfo = BuildStartInfo("/Run", "/TN", taskName);

        var (exitCode, stdOut, stdErr, startFailure) = await RunAsync(startInfo, cancellationToken);
        if (startFailure is not null)
        {
            _logger.LogError(startFailure, "Failed to launch schtasks.exe to trigger '{TaskName}'", taskName);
            return new ScraperTaskRunResult(false, startFailure.Message);
        }

        if (exitCode == 0)
        {
            return new ScraperTaskRunResult(true, null);
        }

        var message = new StringBuilder($"schtasks.exe exited with code {exitCode}.");
        if (!string.IsNullOrWhiteSpace(stdErr))
        {
            message.Append(" stderr: ").Append(stdErr);
        }
        if (!string.IsNullOrWhiteSpace(stdOut))
        {
            message.Append(" stdout: ").Append(stdOut);
        }
        // Disambiguates the most common cause of this failure: were the ScraperTaskRunner
        // credentials actually picked up, or is a stale worker process (started before the
        // environment variables were set, e.g. an app pool recycle without a full iisreset)
        // still running schtasks.exe as the app pool's own identity?
        message.Append(usingConfiguredCredentials
            ? $" (ran as configured account '{_settings.Username}')"
            : " (no ScraperTaskRunner credentials configured/visible - ran as the app pool's own identity)");

        _logger.LogError("schtasks /Run /TN {TaskName} failed: {Message}", taskName, message);
        return new ScraperTaskRunResult(false, message.ToString());
    }

    public async Task<ScraperTaskStatus> GetStatusAsync(string taskName, CancellationToken cancellationToken = default)
    {
        // /FO LIST rather than CSV: one "Field Name:    Value" line per field, no quoting/escaping
        // rules to get wrong for a value that happens to contain a comma.
        var startInfo = BuildStartInfo("/Query", "/TN", taskName, "/V", "/FO", "LIST");

        var (exitCode, stdOut, stdErr, startFailure) = await RunAsync(startInfo, cancellationToken);
        if (startFailure is not null)
        {
            _logger.LogError(startFailure, "Failed to launch schtasks.exe to query '{TaskName}'", taskName);
            return new ScraperTaskStatus(null, null, null, null, startFailure.Message);
        }

        if (exitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(stdErr) ? $"schtasks /Query exited with code {exitCode}." : stdErr;
            _logger.LogError("schtasks /Query /TN {TaskName} failed: {Message}", taskName, message);
            return new ScraperTaskStatus(null, null, null, null, message);
        }

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in stdOut.Split('\n'))
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }
            var name = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().TrimEnd('\r');
            fields[name] = value;
        }

        fields.TryGetValue("Status", out var status);
        fields.TryGetValue("Last Run Time", out var lastRunTime);
        fields.TryGetValue("Last Result", out var lastResult);
        fields.TryGetValue("Next Run Time", out var nextRunTime);
        return new ScraperTaskStatus(status, lastRunTime, lastResult, nextRunTime, null);
    }

    public async Task<bool> EndAsync(string taskName, CancellationToken cancellationToken = default)
    {
        var startInfo = BuildStartInfo("/End", "/TN", taskName);
        var (exitCode, _, stdErr, startFailure) = await RunAsync(startInfo, cancellationToken);
        if (startFailure is not null)
        {
            _logger.LogError(startFailure, "Failed to launch schtasks.exe to end '{TaskName}'", taskName);
            return false;
        }

        if (exitCode != 0)
        {
            _logger.LogWarning("schtasks /End /TN {TaskName} failed (exit {ExitCode}): {StdErr}", taskName, exitCode, stdErr);
        }
        return exitCode == 0;
    }

    private bool HasConfiguredCredentials =>
        !string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password);

    private ProcessStartInfo BuildStartInfo(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (HasConfiguredCredentials)
        {
            startInfo.UserName = _settings.Username;
            // CreateProcessWithLogonW (which ProcessStartInfo.UserName/Password map to) treats an
            // empty Domain as "look this account up on a domain controller" - for a local account
            // like a server's own Administrator, that lookup fails even though the account and
            // password are both correct. "." tells it "local accounts database on this machine"
            // instead, which is what a local Administrator account actually needs.
            startInfo.Domain = string.IsNullOrWhiteSpace(_settings.Domain) ? "." : _settings.Domain;
            startInfo.PasswordInClearText = _settings.Password;
            startInfo.LoadUserProfile = false;
        }

        return startInfo;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr, Exception? StartFailure)> RunAsync(
        ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            // CreateProcessWithLogonW throws (rather than returning a failed process) for bad
            // credentials, a missing "Log on as a batch job" right, etc. - surface the real message
            // instead of a bare false.
            return (-1, string.Empty, string.Empty, ex);
        }

        using (process)
        {
            if (process is null)
            {
                return (-1, string.Empty, string.Empty, new InvalidOperationException("Process.Start returned no process."));
            }

            var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var stdOut = (await stdOutTask).Trim();
            var stdErr = (await stdErrTask).Trim();
            return (process.ExitCode, stdOut, stdErr, null);
        }
    }
}
