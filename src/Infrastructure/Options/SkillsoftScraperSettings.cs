namespace SkillsetsBackend.Infrastructure.Options;

public class SkillsoftScraperSettings
{
    public const string SectionName = "SkillsoftScraper";

    public string PythonExecutablePath { get; set; } = "python";

    public string ScriptPath { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public string SkillportUsername { get; set; } = "talha_admin";

    public string SkillportPassword { get; set; } = string.Empty;

    public int TimeoutMinutes { get; set; } = 180;
}
