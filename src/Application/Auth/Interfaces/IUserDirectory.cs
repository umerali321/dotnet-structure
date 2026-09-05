namespace SkillsetsBackend.Application.Auth.Interfaces;

/// <summary>Read access to the existing legacy Users/Companies/Roles/UserCompanyRoles data.</summary>
public interface IUserDirectory
{
    /// <summary>Finds an active user by email or username (case-insensitive).</summary>
    Task<DirectoryUser?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DirectoryCompanyRole>> GetActiveCompanyRolesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Same as <see cref="GetActiveCompanyRolesAsync"/> (active role assignment, valid
    /// StartDate/EndDate window) but WITHOUT requiring the company itself to be active. Use this to
    /// resolve a TARGET user's companies for admin visibility/authorization-scope checks (e.g. "does
    /// this student's company overlap with the Manager's managed companies") - a company going
    /// inactive must not also make its users invisible/inaccessible to the admins who already have
    /// legitimate access to them. Never use this for login/company-selection - that must keep
    /// requiring an active company (see GetActiveCompanyRolesAsync / QueryActiveCompanyRoles).</summary>
    Task<IReadOnlyList<DirectoryCompanyRole>> GetCompanyRolesIgnoringCompanyStatusAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Gets one specific active company-role membership for an explicit session selection.</summary>
    Task<DirectoryCompanyRole?> GetActiveCompanyRoleAsync(
        int userId,
        int companyId,
        string role,
        CancellationToken cancellationToken = default);

    /// <summary>True if the user has ever had a company-role membership row, regardless of whether
    /// it (or its company) is currently active. Used to distinguish "this user's access was revoked
    /// (company/membership deactivated)" from "this user genuinely has 2+ companies to pick from" -
    /// both otherwise resolve to zero active roles / the Unassigned role.</summary>
    Task<bool> HasAnyCompanyRoleAsync(int userId, CancellationToken cancellationToken = default);
}

public record DirectoryUser(
    int UserId,
    string? Email,
    string? Username,
    string? FirstName,
    string? LastName,
    string? LegacyPasswordValue,
    bool IsActive);

public record DirectoryCompanyRole(
    int CompanyId,
    string CompanyCode,
    string CompanyName,
    byte RoleId,
    string RoleName,
    DateOnly? StartDate,
    DateOnly? EndDate);
