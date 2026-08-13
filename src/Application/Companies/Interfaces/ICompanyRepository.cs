using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Companies.Interfaces;

public interface ICompanyRepository
{
    Task<bool> CompanyCodeExistsAsync(string companyCode, CancellationToken cancellationToken = default);

    Task<bool> IdentifierInUseAsync(string email, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the Companies row, then the admin's Users and UserCompanyRoles rows (with the
    /// "CompanyAdmin" RoleId) referencing the newly generated CompanyId/UserId, atomically.
    /// Returns the new CompanyId.
    /// </summary>
    Task<int> CreateCompanyWithAdminAsync(
        Company company,
        AppUser admin,
        CancellationToken cancellationToken = default);
}
