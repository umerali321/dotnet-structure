using SkillsetsBackend.Application.RoleManagement.DTOs;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.RoleManagement.Interfaces;

public interface IRoleRepository
{
    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleSummaryDto>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleDto?> GetRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default);

    Task<bool> RoleNameExistsAsync(string roleName, CancellationToken cancellationToken = default);

    Task<bool> IsSystemRoleAsync(byte roleId, CancellationToken cancellationToken = default);

    /// <summary>Returns the new Role's RoleId (DB-generated).</summary>
    Task<byte> AddRoleAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>Replaces the full set of permissions assigned to a role (clear + re-add), atomically.</summary>
    Task ReplaceRolePermissionsAsync(byte roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken = default);
}
