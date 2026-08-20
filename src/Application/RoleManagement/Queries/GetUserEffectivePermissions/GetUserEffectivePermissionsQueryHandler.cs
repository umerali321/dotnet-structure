using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RoleManagement.Queries.GetUserEffectivePermissions;

public record UserEffectivePermissionsDto(int UserId, string RoleName, IReadOnlyList<string> PermissionKeys);

/// <summary>SuperAdmin: any user. CompanyAdmin: only users in a company they manage. Resolves the
/// target's *current* role the same way login does (CompanyContextResolver), then looks up that
/// role's effective permissions via IPermissionService.</summary>
public class GetUserEffectivePermissionsQueryHandler
{
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public GetUserEffectivePermissionsQueryHandler(IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<UserEffectivePermissionsDto> Handle(int targetUserId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and Company Admins can view another user's permissions.");
        }

        var targetCompanies = await _userDirectory.GetActiveCompanyRolesAsync(targetUserId, cancellationToken);

        if (!caller.IsSuperAdmin)
        {
            var managedCompanyIds = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);
            if (!targetCompanies.Any(tc => managedCompanyIds.Contains(tc.CompanyId)))
            {
                throw new UnauthorizedAccessException("You do not have access to this user.");
            }
        }

        var (role, _, _) = CompanyContextResolver.Resolve(targetCompanies);
        var permissionKeys = await _permissionService.GetEffectivePermissionKeysForUserAsync(targetUserId, role, cancellationToken);

        return new UserEffectivePermissionsDto(targetUserId, role, permissionKeys);
    }
}
