using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.RoleManagement.DTOs;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.RoleManagement;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.PermissionId)
            .Select(p => new PermissionDto(p.PermissionId, p.PermissionKey, p.Category, p.Description))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RoleSummaryDto>> ListRolesAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleId)
            .Select(r => new RoleSummaryDto(r.RoleId, r.RoleName, r.IsSystemRole, r.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<RoleDto?> GetRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.RoleId == roleId)
            .Select(r => new { r.RoleId, r.RoleName, r.IsSystemRole, r.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        var permissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .OrderBy(rp => rp.PermissionId)
            .Join(_dbContext.Permissions.AsNoTracking(), rp => rp.PermissionId, p => p.PermissionId,
                (rp, p) => new PermissionDto(p.PermissionId, p.PermissionKey, p.Category, p.Description))
            .ToListAsync(cancellationToken);

        return new RoleDto(role.RoleId, role.RoleName, role.IsSystemRole, role.IsActive, permissions);
    }

    public Task<bool> RoleNameExistsAsync(string roleName, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.AsNoTracking().AnyAsync(r => r.RoleName == roleName, cancellationToken);

    public Task<bool> RoleNameExistsAsync(string roleName, byte excludeRoleId, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.AsNoTracking().AnyAsync(r => r.RoleName == roleName && r.RoleId != excludeRoleId, cancellationToken);

    public async Task<bool> IsSystemRoleAsync(byte roleId, CancellationToken cancellationToken = default) =>
        await _dbContext.Roles.AsNoTracking().Where(r => r.RoleId == roleId).Select(r => r.IsSystemRole).FirstOrDefaultAsync(cancellationToken);

    public Task<Role?> GetTrackedRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<byte> AddRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return role.RoleId;
    }

    public async Task ReplaceRolePermissionsAsync(byte roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken = default)
    {
        // EnableRetryOnFailure requires operations wrapped this way - a plain BeginTransactionAsync
        // throws because SqlServerRetryingExecutionStrategy doesn't support user-initiated transactions.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existing = await _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
            _dbContext.RolePermissions.RemoveRange(existing);

            foreach (var permissionId in permissionIds.Distinct())
            {
                _dbContext.RolePermissions.Add(new RolePermission(roleId, permissionId));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task ReplaceUserPermissionOverridesAsync(int userId, IReadOnlyDictionary<int, bool> overridesByPermissionId, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existing = await _dbContext.UserPermissionOverrides.Where(o => o.UserId == userId).ToListAsync(cancellationToken);
            _dbContext.UserPermissionOverrides.RemoveRange(existing);

            foreach (var (permissionId, isGranted) in overridesByPermissionId)
            {
                _dbContext.UserPermissionOverrides.Add(new UserPermissionOverride(userId, permissionId, isGranted, updatedBy));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<IReadOnlyDictionary<int, bool>> GetUserPermissionOverridesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.UserPermissionOverrides
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new { o.PermissionId, o.IsGranted })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.PermissionId, r => r.IsGranted);
    }
}
