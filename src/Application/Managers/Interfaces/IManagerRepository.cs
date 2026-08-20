using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Interfaces;

public interface IManagerRepository
{
    Task<AppUser?> GetUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> IdentifierInUseAsync(string email, string username, int? excludeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the Users row, then the UserCompanyRoles row (with the given role's RoleId - "Manager"
    /// or "CompanyAdmin") referencing the newly generated UserId, atomically. Returns the new UserId.
    /// </summary>
    Task<int> CreateManagerAsync(
        AppUser user,
        int companyId,
        DateOnly? startDate,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a Manager UserCompanyRole to an already-existing user (e.g. a current Employee
    /// being additionally made a Manager at the same company) - no new Users row, unlike
    /// CreateManagerAsync.</summary>
    Task AddManagerRoleAsync(int userId, int companyId, DateOnly? startDate, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
