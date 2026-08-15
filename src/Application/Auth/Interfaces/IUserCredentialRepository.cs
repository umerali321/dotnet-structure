using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Auth.Interfaces;

/// <summary>Narrow write access to AppUser for the login page's self-service password reset -
/// deliberately generic (not Student- or Manager-specific) since a matched email can belong to
/// either.</summary>
public interface IUserCredentialRepository
{
    Task<AppUser?> GetUserAsync(int userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
