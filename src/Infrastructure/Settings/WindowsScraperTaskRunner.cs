using System.Diagnostics;
using System.Runtime.Versioning;
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
/// than leaving warnings on the Domain/PasswordInClearText/LoadUserProfile calls below.</summary>
[SupportedOSPlatform("windows")]
public class WindowsScraperTaskRunner : IScraperTaskRunner
{
    // Must exactly match the task name register_nightly_task.ps1 registers on the server.
    private const string TaskName = "SkillSets - Nightly Learning Transcript Sync";

    private readonly ScraperTaskRunnerSettings _settings;

    public WindowsScraperTaskRunner(IOptions<ScraperTaskRunnerSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<bool> TriggerNowAsync(CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            ArgumentList = { "/Run", "/TN", TaskName },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        // The IIS application pool this API runs under normally has no permission to trigger a task
        // it doesn't own (e.g. one registered under the Administrator account) - Task Scheduler's
        // own security descriptor blocks it, and editing that descriptor by hand is fragile. Running
        // schtasks.exe AS that owning account instead sidesteps the ACL question entirely. Falls
        // back to running as the app pool's own identity when unset (e.g. local dev, where this
        // task doesn't exist anyway).
        if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
        {
            startInfo.UserName = _settings.Username;
            startInfo.Domain = _settings.Domain;
            startInfo.PasswordInClearText = _settings.Password;
            startInfo.LoadUserProfile = false;
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }
}
