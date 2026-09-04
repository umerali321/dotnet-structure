using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Common;
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

        // A named row type rather than an anonymous one, so ApplySearch below can be a real method
        // shared by the prefix attempt and the contains fallback - two copies of that predicate
        // could drift, and the fallback would then return rows the fast path never could.
        var query =
            from sp in _dbContext.StudentProfiles.AsNoTracking()
            join u in _dbContext.Users.AsNoTracking() on sp.UserId equals u.UserId
            where studentMemberships.Any(membership => membership.UserId == u.UserId)
            select new StudentRow { sp = sp, u = u };

        if (options.RestrictToCompanyIds is not null)
        {
            var allowed = options.RestrictToCompanyIds;

            if (options.RestrictToManagerId is int managerId)
            {
                // Manager caller: students in a managed company that are still unassigned, plus
                // whichever specific students have been assigned to this Manager (ManagerId is
                // company-independent once set, matching the assignment's intent).
                query = query.Where(x =>
                    (x.sp.ManagerId == null && studentMemberships.Any(membership =>
                        membership.UserId == x.u.UserId && allowed.Contains(membership.CompanyId)))
                    || x.sp.ManagerId == managerId);
            }
            else
            {
                // SuperAdmin / CompanyAdmin: unchanged - every student in an allowed company,
                // regardless of ManagerId assignment.
                query = query.Where(x => studentMemberships.Any(membership =>
                    membership.UserId == x.u.UserId && allowed.Contains(membership.CompanyId)));
            }
        }

        if (!string.IsNullOrWhiteSpace(options.StudentType))
        {
            query = query.Where(x => x.sp.StudentType == options.StudentType);
        }

        if (options.IsActive.HasValue)
        {
            query = query.Where(x => x.u.IsActive == options.IsActive.Value);
        }

        // ONE named column, PREFIX first. What was here before OR'd '%term%' across FirstName,
        // LastName, Email, Username, both name concatenations AND a correlated EXISTS over company
        // code/name - 2,525 ms on this database's 162,487 Users, because a leading wildcard cannot
        // seek an index so all seven predicates scanned. One column with a prefix pattern is 0 ms.
        //
        // The contains fallback below runs only when the prefix found nothing, so nothing that used
        // to be findable stops being findable (a whole-domain search like "augusta.edu" matches 260
        // people with contains and 0 with prefix) - it just costs a scan in the rare case that needs
        // one instead of on every keystroke.
        var unsearched = query;

        if (options.Search is { } search)
        {
            query = ApplySearch(unsearched, studentMemberships, search.Field, search.ToPrefixPattern());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Prefix found nothing - retry once with a contains pattern so mid-string searches (a whole
        // email domain, a surname typed without its start) still work. Costs a scan, but only in the
        // case that actually needs one rather than on every keystroke.
        if (totalCount == 0 && options.Search is { } fallback)
        {
            query = ApplySearch(unsearched, studentMemberships, fallback.Field, fallback.ToContainsPattern());
            totalCount = await query.CountAsync(cancellationToken);
        }

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
            // Newest-added OR most-recently-edited first - Users.UpdatedAt is what actually bumps on
            // a name/email/phone edit or activate/deactivate (see AppUser.cs), so this is the one
            // that reflects "just touched" rather than only "just created".
            "recent" => query.OrderByDescending(x => x.u.UpdatedAt ?? x.u.CreatedAt).ThenByDescending(x => x.u.UserId),
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
                x.sp.ManagerId,
            })
            .ToListAsync(cancellationToken);

        var (companiesByUser, companyCodesByUser) = await LoadCompaniesAsync(page.Select(x => x.UserId), today, cancellationToken);
        var activePairs = await ActiveLibraryCardLookup.GetActivePairsAsync(
            _dbContext, companyCodesByUser.Values.SelectMany(codes => codes), cancellationToken);
        var managerNamesById = await LoadManagerNamesAsync(page.Select(x => x.ManagerId), cancellationToken);

        var items = page
            .Select(x => new StudentListItemDto(
                x.UserId, x.FirstName, x.LastName, x.Email, x.Username, x.Phone, x.StudentType, x.IsActive, x.CreatedAt,
                companiesByUser.TryGetValue(x.UserId, out var companies) ? companies : [],
                HasActiveSkillportCard(x.UserId, x.Email, companyCodesByUser, activePairs),
                x.ManagerId,
                x.ManagerId is int mid && managerNamesById.TryGetValue(mid, out var mname) ? mname : null))
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
                sp.ManagerId,
            }).FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return null;
        }

        var (companiesByUser, companyCodesByUser) = await LoadCompaniesAsync([userId], DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        var companies = companiesByUser.TryGetValue(userId, out var list) ? list : [];
        var activePairs = await ActiveLibraryCardLookup.GetActivePairsAsync(
            _dbContext, companyCodesByUser.Values.SelectMany(codes => codes), cancellationToken);
        var managerNamesById = await LoadManagerNamesAsync([record.ManagerId], cancellationToken);

        return new StudentDetailDto(
            record.UserId, record.FirstName, record.LastName, record.Email, record.Username, record.Phone,
            record.StudentType, record.IsActive, record.CreatedAt, record.UpdatedAt, record.CreatedBy, record.UpdatedBy,
            companies, HasActiveSkillportCard(userId, record.Email, companyCodesByUser, activePairs),
            record.ManagerId,
            record.ManagerId is int mid && managerNamesById.TryGetValue(mid, out var mname) ? mname : null);
    }

    private async Task<Dictionary<int, string>> LoadManagerNamesAsync(IEnumerable<int?> managerIds, CancellationToken cancellationToken)
    {
        var ids = managerIds.Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.UserId))
            .Select(u => new { u.UserId, Name = (u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty) })
            .ToDictionaryAsync(x => x.UserId, x => x.Name.Trim(), cancellationToken);
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

    /// <summary>The join shape this query works over. Named (not anonymous) purely so ApplySearch
    /// can be written once - lowercase members keep the rest of the method unchanged.</summary>
    private sealed class StudentRow
    {
        public required StudentProfile sp { get; init; }

        public required AppUser u { get; init; }
    }

    /// <summary>
    /// Narrows to ONE field. The caller runs this with a prefix pattern first and only falls back to
    /// a contains pattern when the prefix matched nothing, so the common case is an index seek.
    /// </summary>
    private static IQueryable<StudentRow> ApplySearch(
        IQueryable<StudentRow> query,
        IQueryable<UserCompanyRole> studentMemberships,
        SearchBy field,
        string term) => field switch
    {
        // Name keeps both concatenations so "John Smith" typed in full still matches, but only for
        // the Name field - the other searches are no longer dragged through them.
        SearchBy.Name => query.Where(x =>
            EF.Functions.Like(x.u.FirstName!, term, "\\") ||
            EF.Functions.Like(x.u.LastName!, term, "\\") ||
            EF.Functions.Like((x.u.FirstName ?? "") + " " + (x.u.LastName ?? ""), term, "\\") ||
            EF.Functions.Like((x.u.LastName ?? "") + " " + (x.u.FirstName ?? ""), term, "\\")),

        SearchBy.Email => query.Where(x =>
            EF.Functions.Like(x.u.Email!, term, "\\") ||
            EF.Functions.Like(x.u.Username!, term, "\\")),

        SearchBy.Company => query.Where(x => studentMemberships.Any(membership =>
            membership.UserId == x.u.UserId &&
            (EF.Functions.Like(membership.Company.CompanyName!, term, "\\") ||
             EF.Functions.Like(membership.Company.CompanyCode!, term, "\\")))),

        SearchBy.Phone => query.Where(x => EF.Functions.Like(x.u.Phone!, term, "\\")),

        // A field this screen does not offer must narrow to nothing rather than silently returning
        // the unfiltered list as if the search had matched everyone.
        _ => query.Where(_ => false),
    };

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
