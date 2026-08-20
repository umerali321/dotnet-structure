using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.Auth.Interfaces;

/// <summary>
/// Additive RBAC permission layer - new code calls this alongside (never instead of) the existing
/// company/self scoping in StudentAuthorization.cs. SuperAdmin unconditionally has every permission
/// (it has no Roles row to seed - see AGENTS.md). Everyone else resolves through RolePermissions,
/// keyed by their *current* normalized role name (Roles.Normalize), re-derived fresh each call -
/// same "never trust cached claims" convention as IUserDirectory.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(CallerContext caller, string permissionKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetEffectivePermissionKeysAsync(CallerContext caller, CancellationToken cancellationToken = default);

    /// <summary>Same resolution as the two methods above, but for an arbitrary target role name
    /// (already normalized) rather than the caller - used by "view this user's effective permissions".</summary>
    Task<IReadOnlyList<string>> GetEffectivePermissionKeysForRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default);

    /// <summary>The target user's role-derived permissions with their own UserPermissionOverrides
    /// layered on top (a granted override adds a key even if the role doesn't have it; a revoked
    /// override removes one even if the role does) - the actual per-person answer, used both for
    /// "view/edit this user's permissions" and (via HasPermissionAsync/GetEffectivePermissionKeysAsync
    /// when CallerContext.DbUserId is set) for real enforcement of the caller's own overrides.</summary>
    Task<IReadOnlyList<string>> GetEffectivePermissionKeysForUserAsync(int userId, string normalizedRoleName, CancellationToken cancellationToken = default);
}
