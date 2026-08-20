using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Companies.Interfaces;

public interface ICompanyRepository
{
    Task<bool> CompanyCodeExistsAsync(string companyCode, int? excludeCompanyId = null, CancellationToken cancellationToken = default);

    Task<bool> IdentifierInUseAsync(string email, string username, CancellationToken cancellationToken = default);

    /// <summary>Tracked load, for a command handler to mutate and save.</summary>
    Task<Company?> GetByIdAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Tracked load by CompanyCode (exact) or, failing that, CompanyName (exact,
    /// case-insensitive) - CompanyCode wins if a row matches by code, since it's the more precise
    /// natural key. Used by the Company Import tool's "does this row already exist?" check.</summary>
    Task<Company?> FindExistingAsync(string companyCode, string companyName, CancellationToken cancellationToken = default);

    /// <summary>Tracked load of a Users row by email (case-insensitive) - used by the Company Import
    /// tool to detect "this Point of Contact is already a person in the system" before deciding
    /// whether to create a new user or just grant an existing one a CompanyAdmin role.</summary>
    Task<AppUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> HasActiveCompanyAdminAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Tracked load of this company's active CompanyAdmin, if any (arbitrary pick if somehow
    /// more than one is active).</summary>
    Task<AppUser?> GetCompanyAdminAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the Companies row, then the admin's Users and UserCompanyRoles rows (with the
    /// "CompanyAdmin" RoleId) referencing the newly generated CompanyId/UserId, atomically.
    /// Returns the new CompanyId.
    /// </summary>
    Task<int> CreateCompanyWithAdminAsync(
        Company company,
        AppUser admin,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
