using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Skillsoft;

public class StudentManagerResolver
{
    private readonly ApplicationDbContext _dbContext;

    public StudentManagerResolver(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(string? Email, string? Name)> ResolveAsync(int studentUserId, int companyId, CancellationToken cancellationToken)
    {
        var createdByEmail = await _dbContext.StudentProfiles.AsNoTracking()
            .Where(sp => sp.UserId == studentUserId)
            .Select(sp => sp.CreatedBy)
            .FirstOrDefaultAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeManagers =
            from ucr in _dbContext.UserCompanyRoles.AsNoTracking()
            join u in _dbContext.Users.AsNoTracking() on ucr.UserId equals u.UserId
            where ucr.CompanyId == companyId && ucr.IsActive && ucr.Role.RoleName == "Manager"
                && (ucr.StartDate == null || ucr.StartDate <= today)
                && (ucr.EndDate == null || ucr.EndDate >= today)
            select new { u.Email, u.FirstName, u.LastName };

        if (!string.IsNullOrWhiteSpace(createdByEmail))
        {
            var creator = await activeManagers
                .Where(m => m.Email != null && m.Email.ToLower() == createdByEmail.ToLower())
                .FirstOrDefaultAsync(cancellationToken);

            if (creator is not null)
            {
                return (creator.Email, $"{creator.FirstName} {creator.LastName}".Trim());
            }
        }

        var anyManager = await activeManagers.FirstOrDefaultAsync(cancellationToken);
        return anyManager is null ? (null, null) : (anyManager.Email, $"{anyManager.FirstName} {anyManager.LastName}".Trim());
    }
}
