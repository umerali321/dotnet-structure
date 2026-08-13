using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Interfaces;

public interface IManagerRepository
{
    Task<bool> IdentifierInUseAsync(string email, string username, int? excludeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the Users row, then the UserCompanyRoles row (with the "Manager" RoleId) referencing
    /// the newly generated UserId, atomically. Returns the new UserId.
    /// </summary>
    Task<int> CreateManagerAsync(
        AppUser user,
        int companyId,
        DateOnly? startDate,
        CancellationToken cancellationToken = default);
}
