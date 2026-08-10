namespace SkillsetsBackend.Infrastructure.Options;

public class SkillsoftSsoSettings
{
    public const string SectionName = "SkillsoftSso";

    public string BypassLoginBaseUrl { get; set; } = string.Empty;

    public string BypassLoginRestype { get; set; } = "0";

    public int LaunchTicketExpirySeconds { get; set; } = 60;
}
