using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.LearningTranscript.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.LearningTranscript;

/// <summary>Resolves the scoping restrictions shared by every Learning Transcript query -
/// SuperAdmin unrestricted (with an optional company picker), CompanyAdmin/Manager narrowed to
/// their managed companies (reusing StudentAuthorization.GetManagedCompanyIdsAsync, the same
/// source of truth the Students module uses), and Student/Employee forced to their own record
/// regardless of any filter the client sends.</summary>
public static class LearningTranscriptAuthorization
{
    public static async Task<(IReadOnlyCollection<int>? RestrictToCompanyIds, int? RestrictToManagerId, int? RestrictToUserId)> ResolveScopeAsync(
        CallerContext caller,
        IUserDirectory userDirectory,
        int? companyIdFilter,
        CancellationToken cancellationToken)
    {
        if (caller.HasGlobalCompanyScope)
        {
            var restrict = companyIdFilter.HasValue ? new[] { companyIdFilter.Value } : null;
            return (restrict, null, null);
        }

        if (caller.Role == Roles.Student)
        {
            // An Employee always sees only themself - never widened by any client-supplied filter.
            return (null, null, caller.DbUserId);
        }

        var managed = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);

        IReadOnlyCollection<int> restrictToCompanyIds;
        if (companyIdFilter.HasValue)
        {
            if (!managed.Contains(companyIdFilter.Value))
            {
                throw new UnauthorizedAccessException("You do not have access to that company.");
            }

            restrictToCompanyIds = [companyIdFilter.Value];
        }
        else
        {
            restrictToCompanyIds = managed;
        }

        // Only a plain Manager is narrowed further to their assigned (+ unassigned) employees - a
        // CompanyAdmin sees every employee in their company, matching StudentAuthorization's rule.
        var restrictToManagerId = caller.Role == Roles.Manager ? caller.DbUserId : null;

        return (restrictToCompanyIds, restrictToManagerId, null);
    }
}
