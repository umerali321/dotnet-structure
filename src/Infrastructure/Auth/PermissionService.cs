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

        var keys = await ResolveCallerKeysAsync(caller, cancellationToken);
        return keys.Contains(permissionKey);
    }

    public Task<IReadOnlyList<string>> GetEffectivePermissionKeysAsync(CallerContext caller, CancellationToken cancellationToken = default) =>
        caller.IsSuperAdmin
            ? GetAllPermissionKeysAsync(cancellationToken)
            : ResolveCallerKeysAsync(caller, cancellationToken);

    /// <summary>Per-user overrides only ever apply to a real, identified account - falls back to the
    /// plain role resolution when the caller has no DbUserId (shouldn't happen for an authenticated
    /// non-SuperAdmin caller, but keeps this safe rather than throwing).</summary>
    private Task<IReadOnlyList<string>> ResolveCallerKeysAsync(CallerContext caller, CancellationToken cancellationToken) =>
        caller.DbUserId.HasValue
            ? GetEffectivePermissionKeysForUserAsync(caller.DbUserId.Value, caller.Role, cancellationToken)
            : GetEffectivePermissionKeysForRoleAsync(caller.Role, cancellationToken);

    public async Task<IReadOnlyList<string>> GetEffectivePermissionKeysForUserAsync(int userId, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var roleKeys = await GetEffectivePermissionKeysForRoleAsync(normalizedRoleName, cancellationToken);

        var overrides = await _dbContext.UserPermissionOverrides
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Join(_dbContext.Permissions.AsNoTracking(), o => o.PermissionId, p => p.PermissionId, (o, p) => new { p.PermissionKey, o.IsGranted })
            .ToListAsync(cancellationToken);

        if (overrides.Count == 0)
        {
            return roleKeys;
        }

        var result = new HashSet<string>(roleKeys);
        foreach (var o in overrides)
        {
            if (o.IsGranted)
            {
                result.Add(o.PermissionKey);
            }
            else
            {
                result.Remove(o.PermissionKey);
            }
        }

        return result.ToList();
    }

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
