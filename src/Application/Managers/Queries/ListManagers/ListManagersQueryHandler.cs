using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Managers.Queries.ListManagers;

public sealed class ListManagersQueryHandler(IManagerQueryService service, IUserDirectory directory, IPermissionService permissionService)
{
    public async Task<PaginatedList<ManagerListItemDto>> Handle(
        int page, int pageSize, string? search, int? companyId, bool? active, string? sort, bool descending, string? role,
        CallerContext caller, CancellationToken ct)
    {
        // Permission-driven (RolePermissions), not a hardcoded role check - a SuperAdmin can grant or
        // revoke "View Managers" for the Manager role from the Roles & Permissions screen and this
        // takes effect immediately, no code change needed.
        if (!caller.IsSuperAdmin && !await permissionService.HasPermissionAsync(caller, Permissions.Managers.View, ct))
        {
            throw new UnauthorizedAccessException("You do not have permission to view managers.");
        }

        if (role == "CompanyAdmin" && !caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company admins can list company admins.");
        }

        IReadOnlyCollection<int>? allowed;
        if (caller.IsSuperAdmin)
        {
            allowed = companyId is null ? null : [companyId.Value];
        }
        else
        {
            var managed = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, directory, ct);
            if (companyId is not null && !managed.Contains(companyId.Value))
            {
                throw new UnauthorizedAccessException("You do not have access to that company.");
            }
            allowed = companyId is null ? managed : [companyId.Value];
        }

        return await service.ListAsync(
            new(Math.Max(1, page), pageSize <= 0 ? 50 : Math.Min(200, pageSize), search, active, sort, descending, allowed, role), ct);
    }
}
