using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Auth;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _dbContext;

    public PermissionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasPermissionAsync(CallerContext caller, string permissionKey, CancellationToken cancellationToken = default)
    {
        if (caller.IsSuperAdmin)
        {
            return true;
        }

        var keys = await GetEffectivePermissionKeysForRoleAsync(caller.Role, cancellationToken);
        return keys.Contains(permissionKey);
    }

    public Task<IReadOnlyList<string>> GetEffectivePermissionKeysAsync(CallerContext caller, CancellationToken cancellationToken = default) =>
        caller.IsSuperAdmin
            ? GetAllPermissionKeysAsync(cancellationToken)
            : GetEffectivePermissionKeysForRoleAsync(caller.Role, cancellationToken);

    public async Task<IReadOnlyList<string>> GetEffectivePermissionKeysForRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        if (normalizedRoleName == Roles.SuperAdmin)
        {
            return await GetAllPermissionKeysAsync(cancellationToken);
        }

        var roleIds = await _dbContext.Roles
            .AsNoTracking()
            .Select(r => new { r.RoleId, r.RoleName })
            .ToListAsync(cancellationToken);

        var matchingRoleIds = roleIds
            .Where(r => Roles.Normalize(r.RoleName) == normalizedRoleName)
            .Select(r => r.RoleId)
            .ToList();

        if (matchingRoleIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => matchingRoleIds.Contains(rp.RoleId))
            .Join(_dbContext.Permissions.AsNoTracking(), rp => rp.PermissionId, p => p.PermissionId, (rp, p) => p.PermissionKey)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetAllPermissionKeysAsync(CancellationToken cancellationToken) =>
        await _dbContext.Permissions.AsNoTracking().Select(p => p.PermissionKey).ToListAsync(cancellationToken);
}
