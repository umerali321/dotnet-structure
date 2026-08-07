using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

/// <summary>
/// Maps to the existing "Users" table. New rows are only ever created here for students (via
/// CreateStudent) - there is still no general user-management/registration feature.
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
    /// ILegacyCredentialVerifier for how it is checked. New users are stored the same way for
    /// login to keep working, not because it is the recommended format.
    /// </summary>
    public string? PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private AppUser()
    {
    }

    public static AppUser CreateStudent(
        string email,
        string? phone,
        string firstName,
        string lastName,
        string username,
        string initialLegacyPassword)
    {
        return new AppUser
        {
            Email = email,
            Phone = phone,
            FirstName = firstName,
            LastName = lastName,
            Username = username,
            PasswordHash = initialLegacyPassword,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateProfile(string email, string? phone, string firstName, string lastName, string username)
    {
        Email = email;
        Phone = phone;
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateContactInfo(string email, string? phone)
    {
        Email = email;
        Phone = phone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPassword(string legacyPasswordValue)
    {
        PasswordHash = legacyPasswordValue;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
