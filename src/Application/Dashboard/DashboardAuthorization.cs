using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Dashboard;

/// <summary>Shared caller-authorization + company-scope resolution for every Dashboard query -
/// the dashboard is SuperAdmin/CompanyAdmin only (Manager/Student get 403).</summary>
public static class DashboardAuthorization
{
    /// <summary>Returns null for "no company restriction" (SuperAdmin, no company picked - all
    /// companies), or the resolved set of company ids to restrict to: the single id a SuperAdmin
    /// picked, or - for a CompanyAdmin - the company/companies they manage regardless of what (if
    /// anything) they passed in requestedCompanyId, since a CompanyAdmin can never see another
    /// company's data.</summary>
    public static async Task<IReadOnlyCollection<int>?> ResolveCompanyScopeAsync(
        CallerContext caller,
        int? requestedCompanyId,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        if (caller.IsSuperAdmin)
        {
            return requestedCompanyId.HasValue ? [requestedCompanyId.Value] : null;
        }

        if (caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and CompanyAdmin can view the dashboard.");
        }

        return await StudentAuthorization.GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);
    }
}
