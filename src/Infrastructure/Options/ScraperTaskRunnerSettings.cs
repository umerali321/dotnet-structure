namespace SkillsetsBackend.Infrastructure.Options;

/// <summary>Credentials for the Windows account (e.g. the same Administrator account that already
/// owns the "SkillSets - Nightly Learning Transcript Sync" scheduled task) to run schtasks.exe as,
/// instead of the IIS application pool's own identity - the app pool normally lacks permission to
/// trigger a task it doesn't own, and editing Task Scheduler's security descriptor to grant it that
/// is far more fragile than just running the command as an account that already has it. All three
/// left blank means WindowsScraperTaskRunner falls back to running as the app pool's own identity
/// (today's behaviour) - set via environment variables in production, never committed here.</summary>
public class ScraperTaskRunnerSettings
{
    public const string SectionName = "ScraperTaskRunner";

    public string Domain { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
