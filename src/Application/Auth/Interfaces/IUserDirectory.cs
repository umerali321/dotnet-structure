namespace SkillsetsBackend.Application.Auth.Interfaces;

/// <summary>Read access to the existing legacy Users/Companies/Roles/UserCompanyRoles data.</summary>
public interface IUserDirectory
{
    /// <summary>Finds an active user by email or username (case-insensitive).</summary>
    Task<DirectoryUser?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DirectoryCompanyRole>> GetActiveCompanyRolesAsync(int userId, CancellationToken cancellationToken = default);

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
