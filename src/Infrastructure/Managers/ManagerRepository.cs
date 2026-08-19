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
}
