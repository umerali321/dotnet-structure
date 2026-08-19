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

        // Preserve each active role at a company. A user who is both Student and Manager must
        // explicitly choose which role they are using for the current session.
        return rows
            .GroupBy(x => new { x.CompanyId, Role = Roles.Normalize(x.RoleName) })
            .Select(g => g.First())
            .ToList();
    }

    public async Task<DirectoryCompanyRole?> GetActiveCompanyRoleAsync(
        int userId,
        int companyId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryActiveCompanyRoles(userId, companyId).ToListAsync(cancellationToken);
        return rows.FirstOrDefault(x => Roles.Normalize(x.RoleName) == role);
    }

    public Task<bool> HasAnyCompanyRoleAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.UserCompanyRoles.AsNoTracking().AnyAsync(ucr => ucr.UserId == userId, cancellationToken);

    private IQueryable<DirectoryCompanyRole> QueryActiveCompanyRoles(int userId, int? companyId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return _dbContext.UserCompanyRoles
            .AsNoTracking()
            .Where(ucr => ucr.UserId == userId
                && (companyId == null || ucr.CompanyId == companyId)
                && ucr.IsActive
                && ucr.Company.IsActive
                && ucr.Company.PlanEndDate >= today
                && (ucr.StartDate == null || ucr.StartDate <= today)
                && (ucr.EndDate == null || ucr.EndDate >= today))
            .Select(ucr => new DirectoryCompanyRole(
                ucr.CompanyId, ucr.Company.CompanyCode, ucr.Company.CompanyName, ucr.RoleId, ucr.Role.RoleName, ucr.StartDate, ucr.EndDate));
    }
}
