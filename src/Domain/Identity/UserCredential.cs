namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// Maps to the existing "UserCredentials" table. Currently unused (0 rows) - legacy passwords
/// live on Users.PasswordHash instead. Modeled here so it is checked first and used automatically
/// if it is ever populated, without any code changes elsewhere.
/// </summary>
public class UserCredential
{
    public long UserCredentialId { get; private set; }

    public int UserId { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset PasswordChangedAt { get; private set; }

    public bool IsActive { get; private set; }

    private UserCredential()
    {
    }
}
