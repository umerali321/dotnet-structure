using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.RoleManagement.Queries.GetMyPermissions;

/// <summary>Any authenticated user - their own effective permissions, for frontend menu/route
/// gating (libs/shared/auth/permission.service.ts). Backend endpoints remain the actual authority;
/// this is UX-only, matching the existing app-access.guard.ts convention.</summary>
public class GetMyPermissionsQueryHandler
{
    private readonly IPermissionService _permissionService;

    public GetMyPermissionsQueryHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public Task<IReadOnlyList<string>> Handle(CallerContext caller, CancellationToken cancellationToken) =>
        _permissionService.GetEffectivePermissionKeysAsync(caller, cancellationToken);
}
