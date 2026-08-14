using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Infrastructure.Skillsoft;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Students;

public class StudentQueryService : IStudentQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public StudentQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<StudentListItemDto>> ListAsync(StudentListQueryOptions options, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var studentMemberships = _dbContext.UserCompanyRoles.AsNoTracking().Where(ucr =>
            ucr.IsActive
            && ucr.Company.IsActive
            && ucr.Role.RoleName == Roles.Student
            && (ucr.StartDate == null || ucr.StartDate <= today)
            && (ucr.EndDate == null || ucr.EndDate >= today));

        var query =
            from sp in _dbContext.StudentProfiles.AsNoTracking()
            join u in _dbContext.Users.AsNoTracking() on sp.UserId equals u.UserId
            where studentMemberships.Any(membership => membership.UserId == u.UserId)
            select new { sp, u };

        if (options.RestrictToCompanyIds is not null)
        {
            var allowed = options.RestrictToCompanyIds;
            query = query.Where(x => studentMemberships.Any(membership =>
                membership.UserId == x.u.UserId && allowed.Contains(membership.CompanyId)));
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var term = $"%{EscapeLike(options.Search.Trim())}%";
            query = query.Where(x =>
                EF.Functions.Like(x.u.FirstName, term, "\\") ||
                EF.Functions.Like(x.u.LastName, term, "\\") ||
                EF.Functions.Like(x.u.Email, term, "\\") ||
                EF.Functions.Like(x.u.Username, term, "\\") ||
                studentMemberships.Any(membership => membership.UserId == x.u.UserId &&
                    (EF.Functions.Like(membership.Company.CompanyCode, term, "\\") || EF.Functions.Like(membership.Company.CompanyName, term, "\\"))));
        }

        if (!string.IsNullOrWhiteSpace(options.StudentType))
        {
            query = query.Where(x => x.sp.StudentType == options.StudentType);
        }

        if (options.IsActive.HasValue)
        {
            query = query.Where(x => x.u.IsActive == options.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // A stable tiebreaker (UserId) is always appended last so Skip/Take pagination is
        // deterministic across requests, regardless of which primary sort is requested.
        var ordered = options.SortBy?.ToLowerInvariant() switch
        {
            "firstname" => options.SortDescending
                ? query.OrderByDescending(x => x.u.FirstName).ThenBy(x => x.u.UserId)
                : query.OrderBy(x => x.u.FirstName).ThenBy(x => x.u.UserId),
            "lastname" => options.SortDescending
                ? query.OrderByDescending(x => x.u.LastName).ThenBy(x => x.u.UserId)
                : query.OrderBy(x => x.u.LastName).ThenBy(x => x.u.UserId),
            "email" => options.SortDescending
                ? query.OrderByDescending(x => x.u.Email).ThenBy(x => x.u.UserId)
                : query.OrderBy(x => x.u.Email).ThenBy(x => x.u.UserId),
            "username" => options.SortDescending
                ? query.OrderByDescending(x => x.u.Username).ThenBy(x => x.u.UserId)
                : query.OrderBy(x => x.u.Username).ThenBy(x => x.u.UserId),
            "createdat" => options.SortDescending
                ? query.OrderByDescending(x => x.sp.CreatedAt).ThenBy(x => x.u.UserId)
                : query.OrderBy(x => x.sp.CreatedAt).ThenBy(x => x.u.UserId),
            _ => query.OrderBy(x => x.u.UserId),
        };

        var page = await ordered
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(x => new
            {
                x.u.UserId,
                x.u.FirstName,
                x.u.LastName,
                x.u.Email,
                x.u.Username,
                x.u.Phone,
                x.sp.StudentType,
                x.u.IsActive,
                x.sp.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var (companiesByUser, companyCodesByUser) = await LoadCompaniesAsync(page.Select(x => x.UserId), today, cancellationToken);
        var activePairs = await ActiveLibraryCardLookup.GetActivePairsAsync(
            _dbContext, companyCodesByUser.Values.SelectMany(codes => codes), cancellationToken);

        var items = page
            .Select(x => new StudentListItemDto(
                x.UserId, x.FirstName, x.LastName, x.Email, x.Username, x.Phone, x.StudentType, x.IsActive, x.CreatedAt,
                companiesByUser.TryGetValue(x.UserId, out var companies) ? companies : [],
                HasActiveSkillportCard(x.UserId, x.Email, companyCodesByUser, activePairs)))
            .ToList();

        return new PaginatedList<StudentListItemDto>(items, totalCount, options.Page, options.PageSize);
    }

    public async Task<StudentDetailDto?> GetDetailAsync(int userId, CancellationToken cancellationToken = default)
    {
        var record = await (
            from sp in _dbContext.StudentProfiles.AsNoTracking()
            join u in _dbContext.Users.AsNoTracking() on sp.UserId equals u.UserId
            where u.UserId == userId
            select new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Username,
                u.Phone,
                sp.StudentType,
                u.IsActive,
                sp.CreatedAt,
                sp.UpdatedAt,
                sp.CreatedBy,
                sp.UpdatedBy,
            }).FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return null;
        }

        var (companiesByUser, companyCodesByUser) = await LoadCompaniesAsync([userId], DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        var companies = companiesByUser.TryGetValue(userId, out var list) ? list : [];
        var activePairs = await ActiveLibraryCardLookup.GetActivePairsAsync(
            _dbContext, companyCodesByUser.Values.SelectMany(codes => codes), cancellationToken);

        return new StudentDetailDto(
            record.UserId, record.FirstName, record.LastName, record.Email, record.Username, record.Phone,
            record.StudentType, record.IsActive, record.CreatedAt, record.UpdatedAt, record.CreatedBy, record.UpdatedBy,
            companies, HasActiveSkillportCard(userId, record.Email, companyCodesByUser, activePairs));
    }

    private async Task<(Dictionary<int, IReadOnlyList<StudentCompanyRoleDto>> Companies, Dictionary<int, List<string>> CompanyCodes)> LoadCompaniesAsync(
        IEnumerable<int> userIds, DateOnly today, CancellationToken cancellationToken)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
        {
            return ([], []);
        }

        var rows = await _dbContext.UserCompanyRoles
            .AsNoTracking()
            .Where(ucr => ids.Contains(ucr.UserId)
                && ucr.IsActive
                && ucr.Company.IsActive
                && ucr.Role.RoleName == Roles.Student
                && (ucr.StartDate == null || ucr.StartDate <= today)
                && (ucr.EndDate == null || ucr.EndDate >= today))
            .Select(ucr => new
            {
                ucr.UserId,
                ucr.CompanyId,
                ucr.Company.CompanyName,
                ucr.Company.CompanyCode,
                ucr.Role.RoleName,
                ucr.StartDate,
                ucr.EndDate,
            })
            .ToListAsync(cancellationToken);

        var companies = rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<StudentCompanyRoleDto>)g
                    .Select(x => new StudentCompanyRoleDto(x.CompanyId, x.CompanyCode, x.CompanyName, Roles.Normalize(x.RoleName), x.StartDate, x.EndDate))
                    .ToList());

        var companyCodes = rows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CompanyCode).ToList());

        return (companies, companyCodes);
    }

    private static bool HasActiveSkillportCard(
        int userId, string? email, Dictionary<int, List<string>> companyCodesByUser, HashSet<(string CompanyCode, string EmailLower)> activePairs)
    {
        if (string.IsNullOrWhiteSpace(email) || !companyCodesByUser.TryGetValue(userId, out var codes))
        {
            return false;
        }

        var emailLower = email.ToLower();
        return codes.Any(code => activePairs.Contains((code, emailLower)));
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
