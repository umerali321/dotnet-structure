using SkillsetsBackend.Domain.Common;

namespace SkillsetsBackend.Domain.Identity;

public class RefreshToken : BaseEntity, IAggregateRoot
{
    public string Token { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? ReplacedByToken { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken()
    {
    }

    public RefreshToken(string token, Guid userId, string email, string role, DateTimeOffset expiresAt, string? createdByIp)
    {
        Token = token;
        UserId = userId;
        Email = email;
        Role = role;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke(string? revokedByIp, string? replacedByToken = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
    }
}
