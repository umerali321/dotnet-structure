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
}
