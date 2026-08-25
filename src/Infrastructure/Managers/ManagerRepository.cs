using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Managers;

public class ManagerRepository : IManagerRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ManagerRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<bool> IdentifierInUseAsync(string email, string username, int? excludeUserId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(
            u => (u.Email == email || u.Username == username) && (excludeUserId == null || u.UserId != excludeUserId),
            cancellationToken);

    public async Task<int> CreateManagerAsync(
        AppUser user,
        int companyId,
        DateOnly? startDate,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var managerRoleId = await _dbContext.Roles
            .Where(r => r.RoleName == roleName)
            .Select(r => r.RoleId)
            .FirstAsync(cancellationToken);

        // EnableRetryOnFailure requires operations wrapped this way - a plain BeginTransactionAsync
        // throws because SqlServerRetryingExecutionStrategy doesn't support user-initiated transactions.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Users.UserId is DB-generated; UserCompanyRole needs it, so the user is saved first to
            // obtain it before the dependent row can be constructed.
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var membership = new UserCompanyRole(user.UserId, companyId, managerRoleId, startDate);
            _dbContext.UserCompanyRoles.Add(membership);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return user.UserId;
        });
    }

    public async Task AddManagerRoleAsync(int userId, int companyId, DateOnly? startDate, CancellationToken cancellationToken = default)
    {
        var managerRoleId = await _dbContext.Roles
            .Where(r => r.RoleName == Domain.Identity.Roles.Manager)
            .Select(r => r.RoleId)
            .FirstAsync(cancellationToken);

        await AddOrReactivateRoleAsync(userId, companyId, managerRoleId, startDate, cancellationToken);
    }

    public async Task AddCompanyAdminRoleAsync(int userId, int companyId, DateOnly? startDate, CancellationToken cancellationToken = default)
    {
        var companyAdminRoleId = await _dbContext.Roles
            .Where(r => r.RoleName == Domain.Identity.Roles.CompanyAdmin)
            .Select(r => r.RoleId)
            .FirstAsync(cancellationToken);

        await AddOrReactivateRoleAsync(userId, companyId, companyAdminRoleId, startDate, cancellationToken);
    }

    /// <summary>UX_UserCompanyRoles_User_Company_Role uniquely constrains (UserId, CompanyId, RoleId)
    /// with no IsActive filter, so a person who once held this exact role at this company (even long
    /// since revoked) already has a row for that triple - reactivate it instead of inserting a
    /// second one, which would violate the index.</summary>
    private async Task AddOrReactivateRoleAsync(int userId, int companyId, byte roleId, DateOnly? startDate, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.UserCompanyRoles
            .FirstOrDefaultAsync(ucr => ucr.UserId == userId && ucr.CompanyId == companyId && ucr.RoleId == roleId, cancellationToken);

        if (existing is not null)
        {
            existing.Reactivate(startDate);
        }
        else
        {
            _dbContext.UserCompanyRoles.Add(new UserCompanyRole(userId, companyId, roleId, startDate));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveManagerRoleAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeRoles = await _dbContext.UserCompanyRoles
            .Where(ucr => ucr.UserId == userId && ucr.CompanyId == companyId && ucr.IsActive && ucr.Role.RoleName == Domain.Identity.Roles.Manager)
            .ToListAsync(cancellationToken);

        foreach (var role in activeRoles)
        {
            role.Deactivate(today);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveCompanyAdminRoleAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeRoles = await _dbContext.UserCompanyRoles
            .Where(ucr => ucr.UserId == userId && ucr.CompanyId == companyId && ucr.IsActive && ucr.Role.RoleName == Domain.Identity.Roles.CompanyAdmin)
            .ToListAsync(cancellationToken);

        foreach (var role in activeRoles)
        {
            role.Deactivate(today);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
