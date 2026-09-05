using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Auth.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes every still-active (not already revoked/expired) refresh token for a user -
    /// called when an admin changes that user's Email, so an already-logged-in session can't keep
    /// silently refreshing itself with the OLD email baked into its claims forever (RefreshToken
    /// rows carry a point-in-time Email snapshot, re-copied forward on every refresh rather than
    /// re-read from Users). Forces a fresh login, which always re-reads Users.Email correctly.</summary>
    Task RevokeAllActiveForUserAsync(string userId, string? revokedByIp, CancellationToken cancellationToken = default);
}
