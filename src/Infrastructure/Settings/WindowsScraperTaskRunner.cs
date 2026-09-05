using System.Diagnostics;
using SkillsetsBackend.Application.Settings.Interfaces;

namespace SkillsetsBackend.Infrastructure.Settings;

/// <summary>Shells out to schtasks.exe rather than a Task Scheduler library - one command, no new
/// package, and it's the exact same mechanism an admin already uses manually
/// (Start-ScheduledTask/schtasks /Run) so behaviour matches what's already been tested in
/// production. Windows-only by construction: this app is only ever deployed to the Windows Server
/// that also runs the scheduled task itself, so there is no cross-platform concern here.</summary>
public class WindowsScraperTaskRunner : IScraperTaskRunner
{
    // Must exactly match the task name register_nightly_task.ps1 registers on the server.
    private const string TaskName = "SkillSets - Nightly Learning Transcript Sync";

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

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }
}
