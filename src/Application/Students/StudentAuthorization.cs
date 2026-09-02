using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Students;

/// <summary>
/// Centralizes the multi-tenant student access rules. Company access is always re-derived from
/// UserCompanyRoles via IUserDirectory (the source of truth), never trusted from client input or
/// cached token claims - a Manager's authorization is their *current* set of active companies.
/// </summary>
public static class StudentAuthorization
{
    /// <summary>SuperAdmin: any student. Manager: students in a managed company - or, once an employee is
    /// explicitly assigned to a specific Manager (StudentProfile.ManagerId), only that Manager. Student: only themself.</summary>
    public static async Task EnsureCanViewStudentAsync(
        CallerContext caller,
        int targetUserId,
        IUserDirectory userDirectory,
        IStudentRepository studentRepository,
        CancellationToken cancellationToken)
    {
        // HasGlobalCompanyScope, not IsSuperAdmin: a SystemAdmin is not confined to one company
        // either. This is only about REACH - whether they may do the thing at all is decided by the
        // permission check in the calling handler.
        if (caller.HasGlobalCompanyScope)
        {
            return;
        }

        if (caller.DbUserId == targetUserId)
        {
            return;
        }

        if (caller.Role == Roles.Manager)
        {
            var assignedManagerId = await studentRepository.GetManagerIdAsync(targetUserId, cancellationToken);
            if (assignedManagerId is not null)
            {
                if (assignedManagerId != caller.DbUserId)
                {
                    throw new UnauthorizedAccessException("You do not have access to this student.");
                }

                return;
            }
        }

        var managedCompanyIds = await GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);
        var targetCompanies = await userDirectory.GetActiveCompanyRolesAsync(targetUserId, cancellationToken);

        if (!targetCompanies.Any(tc => managedCompanyIds.Contains(tc.CompanyId)))
        {
            throw new UnauthorizedAccessException("You do not have access to this student.");
        }
    }

    /// <summary>SuperAdmin: any student. Manager: students in a managed company - or, once an employee is
    /// explicitly assigned to a specific Manager (StudentProfile.ManagerId), only that Manager. Never the student themself.</summary>
    public static async Task EnsureCanManageStudentAsync(
        CallerContext caller,
        int targetUserId,
        IUserDirectory userDirectory,
        IStudentRepository studentRepository,
        CancellationToken cancellationToken)
    {
        // HasGlobalCompanyScope, not IsSuperAdmin: a SystemAdmin is not confined to one company
        // either. This is only about REACH - whether they may do the thing at all is decided by the
        // permission check in the calling handler.
        if (caller.HasGlobalCompanyScope)
        {
            return;
        }

        if (caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("You are not authorized to perform this action.");
        }

        // Only a plain Manager is narrowed to a specific employee's assigned ManagerId - a
        // CompanyAdmin manages every employee in their company regardless of assignment, matching
        // StudentQueryService's list-filter design.
        if (caller.Role == Roles.Manager)
        {
            var assignedManagerId = await studentRepository.GetManagerIdAsync(targetUserId, cancellationToken);
            if (assignedManagerId is not null)
            {
                if (assignedManagerId != caller.DbUserId)
                {
                    throw new UnauthorizedAccessException("You do not have access to this student.");
                }

                return;
            }
        }

        var managedCompanyIds = await GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);
        var targetCompanies = await userDirectory.GetActiveCompanyRolesAsync(targetUserId, cancellationToken);

        if (!targetCompanies.Any(tc => managedCompanyIds.Contains(tc.CompanyId)))
        {
            throw new UnauthorizedAccessException("You do not have access to this student.");
        }
    }

    /// <summary>SuperAdmin: any manager. Manager or CompanyAdmin: managers in a managed company.
    /// Deliberately separate from EnsureCanManageStudentAsync (not reused) - CompanyAdmin must be
    /// able to manage Managers without also gaining access to manage Students.</summary>
    public static async Task EnsureCanManageManagerAsync(
        CallerContext caller,
        int targetUserId,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        // HasGlobalCompanyScope, not IsSuperAdmin: a SystemAdmin is not confined to one company
        // either. This is only about REACH - whether they may do the thing at all is decided by the
        // permission check in the calling handler.
        if (caller.HasGlobalCompanyScope)
        {
            return;
        }

        if (caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("You are not authorized to perform this action.");
        }

        var managedCompanyIds = await GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);
        var targetCompanies = await userDirectory.GetActiveCompanyRolesAsync(targetUserId, cancellationToken);

        if (!targetCompanies.Any(tc => managedCompanyIds.Contains(tc.CompanyId)))
        {
            throw new UnauthorizedAccessException("You do not have access to this manager.");
        }
    }

    /// <summary>Throws unless companyId is one the caller (a Manager) is actively authorized to manage.</summary>
    public static async Task EnsureCanManageCompanyAsync(
        CallerContext caller,
        int companyId,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        // HasGlobalCompanyScope, not IsSuperAdmin: a SystemAdmin is not confined to one company
        // either. This is only about REACH - whether they may do the thing at all is decided by the
        // permission check in the calling handler.
        if (caller.HasGlobalCompanyScope)
        {
            return;
        }

        var managedCompanyIds = await GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);
        if (!managedCompanyIds.Contains(companyId))
        {
            throw new UnauthorizedAccessException("You do not have access to that company.");
        }
    }

    public static async Task<IReadOnlyCollection<int>> GetManagedCompanyIdsAsync(
        CallerContext caller,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        if (caller.DbUserId is null)
        {
            return [];
        }

        var roles = await userDirectory.GetActiveCompanyRolesAsync(caller.DbUserId.Value, cancellationToken);
        return roles
            .Where(r => Roles.Normalize(r.RoleName) == Roles.Manager || r.RoleName == Roles.CompanyAdmin)
            .Select(r => r.CompanyId)
            .ToList();
    }
}
