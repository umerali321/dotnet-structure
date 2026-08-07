using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Auth;

public static class CompanyContextResolver
{
    public const string UnassignedRole = "Unassigned";

    /// <summary>
    /// Resolves the effective role, current company (auto-selected only when the user has
    /// exactly one active company), and the full list of active companies for a user.
    /// </summary>
    public static (string Role, CompanyDto? Current, IReadOnlyList<CompanyDto> Companies) Resolve(
        IReadOnlyList<DirectoryCompanyRole> activeRoles)
    {
        var companies = activeRoles
            .Select(x => new CompanyDto(x.CompanyId, x.CompanyName, Roles.Normalize(x.RoleName)))
            .ToList();

        if (companies.Count == 1)
        {
            var only = companies[0];
            return (only.Role, only, companies);
        }

        return (UnassignedRole, null, companies);
    }
}
