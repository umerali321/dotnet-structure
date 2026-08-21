namespace SkillsetsBackend.Infrastructure.Options;

public class SuperAdminSettings
{
    public const string SectionName = "SuperAdmin";

    /// <summary>Every SuperAdmin login (there is no DB row for these - they're config-only
    /// identities checked before the normal user lookup, see SuperAdminAuthenticator).</summary>
    public List<SuperAdminAccount> Accounts { get; set; } = new();
}

public class SuperAdminAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
