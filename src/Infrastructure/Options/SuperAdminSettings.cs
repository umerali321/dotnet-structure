namespace SkillsetsBackend.Infrastructure.Options;

public class SuperAdminSettings
{
    public const string SectionName = "SuperAdmin";

    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
