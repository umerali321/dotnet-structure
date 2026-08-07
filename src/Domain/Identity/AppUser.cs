using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// Maps to the existing "Users" table. Read-only for now - this phase authenticates against
/// legacy data, it does not create or edit users.
/// </summary>
public class AppUser : IAggregateRoot
{
    public int UserId { get; private set; }

    public int? LegacyUserId { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? Username { get; private set; }

    /// <summary>
    /// Legacy credential value. Despite the name, this is not a cryptographic hash in the
    /// existing data (observed values are 2-10 character plaintext PINs/passwords) - see
    /// ILegacyCredentialVerifier for how it is checked.
    /// </summary>
    public string? PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private AppUser()
    {
    }
}
