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
            .Select(r => new RoleSummaryDto(r.RoleId, r.RoleName, r.IsSystemRole))
            .ToListAsync(cancellationToken);

    public async Task<RoleDto?> GetRoleByIdAsync(byte roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.RoleId == roleId)
            .Select(r => new { r.RoleId, r.RoleName, r.IsSystemRole })
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

        return new RoleDto(role.RoleId, role.RoleName, role.IsSystemRole, permissions);
    }

    public Task<bool> RoleNameExistsAsync(string roleName, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.AsNoTracking().AnyAsync(r => r.RoleName == roleName, cancellationToken);

    public async Task<bool> IsSystemRoleAsync(byte roleId, CancellationToken cancellationToken = default) =>
        await _dbContext.Roles.AsNoTracking().Where(r => r.RoleId == roleId).Select(r => r.IsSystemRole).FirstOrDefaultAsync(cancellationToken);

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
}
