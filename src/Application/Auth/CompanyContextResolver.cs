using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Auth;

public static class CompanyContextResolver
{
    public const string UnassignedRole = "Unassigned";

    /// <summary>
    /// Resolves the effective role, current company, and all active company-role choices.
    /// Auto-selects when there's exactly one active membership, or when every membership is at the
    /// same company (e.g. a Manager who is also enrolled as a Student there) - in that case the
    /// ambiguity is only about which role to use, not which company, so this prefers Manager and
    /// skips the company picker; the full list is still returned so the in-dashboard role switcher
    /// (SwitchCompanyCommand, same company/different role) keeps working exactly as before.
    /// Genuine multi-company ambiguity (different CompanyIds) still requires the picker - unchanged.
    /// </summary>
    public static (string Role, CompanyDto? Current, IReadOnlyList<CompanyDto> Companies) Resolve(
        IReadOnlyList<DirectoryCompanyRole> activeRoles)
    {
        var companies = activeRoles
            .Select(x => new CompanyDto(x.CompanyId, x.CompanyCode, x.CompanyName, Roles.Normalize(x.RoleName)))
            .ToList();

        if (companies.Count == 1)
        {
            var only = companies[0];
            return (only.Role, only, companies);
        }

        var distinctCompanyIds = companies.Select(c => c.CompanyId).Distinct().ToList();
        if (distinctCompanyIds.Count == 1)
        {
            var preferred = companies.FirstOrDefault(c => c.Role == Roles.Manager) ?? companies[0];
            return (preferred.Role, preferred, companies);
        }

        return (UnassignedRole, null, companies);
    }
}
