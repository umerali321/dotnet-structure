using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Auth;

public class UserDirectory : IUserDirectory
{
    private readonly ApplicationDbContext _dbContext;

    public UserDirectory(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DirectoryUser?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim();

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Email == normalized || u.Username == normalized)
            .Select(u => new { u.UserId, u.Email, u.Username, u.FirstName, u.LastName, u.PasswordHash, u.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        // UserCredentials is currently unpopulated in the existing database; when it is used,
        // its value takes priority over the legacy Users.PasswordHash column automatically.
        var credentialPassword = await _dbContext.UserCredentials
            .AsNoTracking()
            .Where(c => c.UserId == user.UserId && c.IsActive)
            .OrderByDescending(c => c.PasswordChangedAt)
            .Select(c => c.PasswordHash)
            .FirstOrDefaultAsync(cancellationToken);

        var legacyPasswordValue = credentialPassword ?? user.PasswordHash;

        return new DirectoryUser(user.UserId, user.Email, user.Username, user.FirstName, user.LastName, legacyPasswordValue, user.IsActive);
    }

    public async Task<IReadOnlyList<DirectoryCompanyRole>> GetActiveCompanyRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryActiveCompanyRoles(userId, companyId: null).ToListAsync(cancellationToken);

        // A user can have more than one active role at the same company (e.g. Student and
        // Manager); collapse to one entry per company, preferring the higher-privilege role.
        return rows
            .GroupBy(x => x.CompanyId)
            .Select(g => g.OrderByDescending(x => RolePriority(x.RoleName)).First())
            .ToList();
    }

    public async Task<DirectoryCompanyRole?> GetActiveCompanyRoleAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryActiveCompanyRoles(userId, companyId).ToListAsync(cancellationToken);
        return rows.OrderByDescending(x => RolePriority(x.RoleName)).FirstOrDefault();
    }

    private IQueryable<DirectoryCompanyRole> QueryActiveCompanyRoles(int userId, int? companyId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return _dbContext.UserCompanyRoles
            .AsNoTracking()
            .Where(ucr => ucr.UserId == userId
                && (companyId == null || ucr.CompanyId == companyId)
                && ucr.IsActive
                && ucr.Company.IsActive
                && (ucr.StartDate == null || ucr.StartDate <= today)
                && (ucr.EndDate == null || ucr.EndDate >= today))
            .Select(ucr => new DirectoryCompanyRole(ucr.CompanyId, ucr.Company.CompanyName, ucr.RoleId, ucr.Role.RoleName));
    }

    private static int RolePriority(string dbRoleName) => Roles.Normalize(dbRoleName) switch
    {
        Roles.Manager => 2,
        Roles.Student => 1,
        _ => 0,
    };
}
