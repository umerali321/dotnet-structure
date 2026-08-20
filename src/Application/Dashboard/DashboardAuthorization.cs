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
    /// picked, or - for a CompanyAdmin (and, for Course Library specifically, a Manager too when
    /// allowManager is set) - the company/companies they manage regardless of what (if anything)
    /// they passed in requestedCompanyId, since neither can ever see another company's data.
    /// allowManager only widens who is *authorized*; it does not by itself narrow a Manager down to
    /// their own team - see DashboardQueryService's restrictToManagerId for that per-person scoping,
    /// mirroring ListStudentsQueryHandler's identical restrictToManagerId pattern.</summary>
    public static async Task<IReadOnlyCollection<int>?> ResolveCompanyScopeAsync(
        CallerContext caller,
        int? requestedCompanyId,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken,
        bool allowManager = false)
    {
        if (caller.IsSuperAdmin)
        {
            return requestedCompanyId.HasValue ? [requestedCompanyId.Value] : null;
        }

        var isAllowedRole = caller.Role == Roles.CompanyAdmin || (allowManager && caller.Role == Roles.Manager);
        if (!isAllowedRole)
        {
            throw new UnauthorizedAccessException(allowManager
                ? "Only SuperAdmin, CompanyAdmin, and Manager can view this."
                : "Only SuperAdmin and CompanyAdmin can view the dashboard.");
        }

        return await StudentAuthorization.GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);
    }
}
