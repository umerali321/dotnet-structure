using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Students;

public class StudentRepository : IStudentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StudentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public Task<StudentProfile?> GetProfileByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.StudentProfiles.FirstOrDefaultAsync(sp => sp.UserId == userId, cancellationToken);

    public Task<int?> GetManagerIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.StudentProfiles.Where(sp => sp.UserId == userId).Select(sp => sp.ManagerId).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IdentifierInUseAsync(string email, string username, int? excludeUserId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(
            u => (u.Email == email || u.Username == username) && (excludeUserId == null || u.UserId != excludeUserId),
            cancellationToken);

    public async Task<int> CreateStudentAsync(
        AppUser user,
        string? studentType,
        string createdBy,
        int companyId,
        DateOnly? startDate,
        CancellationToken cancellationToken = default)
    {
        var studentRoleId = await _dbContext.Roles
            .Where(r => r.RoleName == "Student")
            .Select(r => r.RoleId)
            .FirstAsync(cancellationToken);

        // EnableRetryOnFailure requires operations wrapped this way - a plain BeginTransactionAsync
        // throws because SqlServerRetryingExecutionStrategy doesn't support user-initiated transactions.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Users.UserId is DB-generated; StudentProfile/UserCompanyRole need it, so the user is
            // saved first to obtain it before the dependent rows can be constructed.
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var profile = new StudentProfile(user.UserId, studentType, createdBy);
            var membership = new UserCompanyRole(user.UserId, companyId, studentRoleId, startDate);

            _dbContext.StudentProfiles.Add(profile);
            _dbContext.UserCompanyRoles.Add(membership);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return user.UserId;
        });
    }

    public async Task AddEmployeeRoleAsync(int userId, int companyId, string createdBy, DateOnly? startDate, CancellationToken cancellationToken = default)
    {
        var studentRoleId = await _dbContext.Roles
            .Where(r => r.RoleName == "Student")
            .Select(r => r.RoleId)
            .FirstAsync(cancellationToken);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingProfile = await _dbContext.StudentProfiles.FirstOrDefaultAsync(sp => sp.UserId == userId, cancellationToken);
            if (existingProfile is null)
            {
                _dbContext.StudentProfiles.Add(new StudentProfile(userId, null, createdBy));
            }

            var membership = new UserCompanyRole(userId, companyId, studentRoleId, startDate);
            _dbContext.UserCompanyRoles.Add(membership);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
