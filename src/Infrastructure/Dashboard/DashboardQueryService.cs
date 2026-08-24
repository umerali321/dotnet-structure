using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Dashboard.Dtos;
using SkillsetsBackend.Application.Dashboard.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Dashboard;

/// <summary>
/// GetStatsAsync is backed by a stored procedure (see below). The list/history queries below it are
/// still built as server-translatable IQueryables (COUNT DISTINCT / EXISTS-subquery shapes) rather
/// than materializing user-id lists into memory first - the Users/UserCompanyRoles tables are large
/// enough (100k+ rows) that a client-evaluated ".Contains(bigList)" would blow past SQL Server's
/// ~2100 parameter limit. ActiveLibraryCards is the one exception - it's a small, fixed ~4.5k-row
/// table, so pulling a filtered slice into memory to group by (Email, CompanyCode) is safe and
/// simpler than fighting EF's grouped-aggregate SQL translation.
/// </summary>
public class DashboardQueryService : IDashboardQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Backed by dbo.sp_GetDashboardStats (see Persistence/Migrations -
    /// CreateDashboardStatsStoredProcedure) rather than ~14 sequential LINQ COUNT queries - this
    /// endpoint loads on every admin dashboard visit, so all the buckets are computed server-side
    /// in one round trip instead of one network/query round trip per KPI card.</summary>
    public async Task<DashboardStatsDto> GetStatsAsync(
        IReadOnlyCollection<int>? companyIds,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        // Distinguish "no restriction" (null -> NULL param) from "restricted to zero companies"
        // (empty collection -> empty-string param, which the proc treats as matching nothing).
        var restrictParam = companyIds is null ? null : string.Join(",", companyIds);

        var rows = await _dbContext.Database
            .SqlQuery<DashboardStatsRow>(
                $"EXEC dbo.sp_GetDashboardStats @RestrictToCompanyIds={restrictParam}, @StartDate={startDate}, @EndDate={endDate}")
            .ToListAsync(cancellationToken);

        var row = rows[0];
        return new DashboardStatsDto(
            row.TotalCompanies, row.TotalCompanyAdmins, row.TotalManagers, row.TotalEmployees,
            row.TrialCompanies, row.LicensedCompanies, row.InactiveCompanies, row.ExpiringLicensesIn30Days,
            row.ItEmployees, row.NonItEmployees, row.CourseLibraryUsers,
            row.CompaniesAddedInPeriod, row.UsersAddedInPeriod, row.CourseLibrarySessionsStartedInPeriod);
    }

    private sealed record DashboardStatsRow(
        int TotalCompanies, int TotalCompanyAdmins, int TotalManagers, int TotalEmployees,
        int TrialCompanies, int LicensedCompanies, int InactiveCompanies, int ExpiringLicensesIn30Days,
        int ItEmployees, int NonItEmployees, int CourseLibraryUsers,
        int CompaniesAddedInPeriod, int UsersAddedInPeriod, int CourseLibrarySessionsStartedInPeriod);

    public async Task<PaginatedList<CourseLibraryUserDto>> GetCourseLibraryUsersAsync(
        IReadOnlyCollection<int>? companyIds,
        DateOnly? startDate,
        DateOnly? endDate,
        string? search,
        int page,
        int pageSize,
        int? restrictToManagerId,
        CancellationToken cancellationToken)
    {
        var companyCodes = await ResolveCompanyCodesAsync(companyIds, cancellationToken);

        var cardsQuery = _dbContext.ActiveLibraryCards.AsNoTracking().Where(c => c.Email != null);
        if (companyCodes is not null)
        {
            cardsQuery = cardsQuery.Where(c => companyCodes.Contains(c.CompanyCode));
        }
        if (restrictToManagerId.HasValue)
        {
            var allowedEmails = VisibleEmailsForManagerQuery(restrictToManagerId.Value, companyIds);
            cardsQuery = cardsQuery.Where(c => allowedEmails.Contains(c.Email!.ToLower()));
        }
        if (startDate.HasValue)
        {
            var start = startDate.Value.ToDateTime(TimeOnly.MinValue);
            cardsQuery = cardsQuery.Where(c => c.StartDate >= start);
        }
        if (endDate.HasValue)
        {
            var end = endDate.Value.ToDateTime(TimeOnly.MaxValue);
            cardsQuery = cardsQuery.Where(c => c.StartDate <= end);
        }

        var cards = await cardsQuery
            .Select(c => new { c.CompanyCode, c.CompanyName, c.FirstName, c.LastName, Email = c.Email!, c.StartDate, c.EndDate })
            .ToListAsync(cancellationToken);

        var grouped = cards
            .GroupBy(c => (EmailLower: c.Email.ToLowerInvariant(), c.CompanyCode))
            .Select(g =>
            {
                var latest = g.OrderByDescending(c => c.StartDate).First();
                return new
                {
                    g.Key.EmailLower,
                    g.Key.CompanyCode,
                    Email = latest.Email,
                    CardFirstName = latest.FirstName,
                    CardLastName = latest.LastName,
                    CardCompanyName = latest.CompanyName,
                    LatestStart = latest.StartDate,
                    LatestEnd = latest.EndDate,
                    SessionCount = g.Count(),
                };
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            grouped = grouped
                .Where(g =>
                    g.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || $"{g.CardFirstName} {g.CardLastName}".Contains(term, StringComparison.OrdinalIgnoreCase)
                    || g.CardCompanyName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = grouped.Count;
        var pageItems = grouped
            .OrderByDescending(g => g.LatestStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Batch-resolve registered-user/company data for just this page (not the whole grouped set).
        var emailLowers = pageItems.Select(g => g.EmailLower).Distinct().ToList();
        var matchedUsers = await _dbContext.Users.AsNoTracking()
            .Where(u => u.Email != null && emailLowers.Contains(u.Email.ToLower()))
            .Select(u => new { u.UserId, Email = u.Email!.ToLower(), u.FirstName, u.LastName, u.Phone })
            .ToListAsync(cancellationToken);
        var usersByEmail = matchedUsers.GroupBy(u => u.Email).ToDictionary(g => g.Key, g => g.First());

        var matchedUserIds = matchedUsers.Select(u => u.UserId).ToList();
        var studentTypesByUserId = matchedUserIds.Count == 0
            ? []
            : await _dbContext.StudentProfiles.AsNoTracking()
                .Where(sp => matchedUserIds.Contains(sp.UserId))
                .ToDictionaryAsync(sp => sp.UserId, sp => sp.StudentType, cancellationToken);

        var codes = pageItems.Select(g => g.CompanyCode).Distinct().ToList();
        var companiesByCode = await _dbContext.Companies.AsNoTracking()
            .Where(c => codes.Contains(c.CompanyCode))
            .ToDictionaryAsync(c => c.CompanyCode, c => new { c.CompanyId, c.CompanyName }, cancellationToken);

        var now = DateTime.UtcNow;
        var items = pageItems.Select(g =>
        {
            usersByEmail.TryGetValue(g.EmailLower, out var user);
            companiesByCode.TryGetValue(g.CompanyCode, out var company);
            var fullName = user is not null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : $"{g.CardFirstName} {g.CardLastName}".Trim();
            var studentType = user is not null && studentTypesByUserId.TryGetValue(user.UserId, out var st) ? st : null;
            var status = now >= g.LatestStart && now <= g.LatestEnd ? "Active" : "Expired";

            return new CourseLibraryUserDto(
                g.Email, fullName, user?.UserId, company?.CompanyName ?? g.CardCompanyName, company?.CompanyId,
                studentType, g.LatestStart, g.LatestEnd, status, g.SessionCount, user?.Phone);
        }).ToList();

        return new PaginatedList<CourseLibraryUserDto>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<CourseLibrarySessionDto>> GetSessionHistoryAsync(
        string email,
        IReadOnlyCollection<int>? companyIds,
        int? restrictToManagerId,
        CancellationToken cancellationToken)
    {
        var companyCodes = await ResolveCompanyCodesAsync(companyIds, cancellationToken);
        var emailLower = email.Trim().ToLowerInvariant();

        var query = _dbContext.ActiveLibraryCards.AsNoTracking()
            .Where(c => c.Email != null && c.Email.ToLower() == emailLower);
        if (companyCodes is not null)
        {
            query = query.Where(c => companyCodes.Contains(c.CompanyCode));
        }
        if (restrictToManagerId.HasValue)
        {
            // Same visibility rule as the list - a Manager can only pull history for their own
            // record or an employee visible to them, even if they craft the request directly.
            var allowedEmails = VisibleEmailsForManagerQuery(restrictToManagerId.Value, companyIds);
            query = query.Where(c => allowedEmails.Contains(c.Email!.ToLower()));
        }

        var rows = await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new { c.StartDate, c.EndDate })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return rows
            .Select(r => new CourseLibrarySessionDto(r.StartDate, r.EndDate, now >= r.StartDate && now <= r.EndDate ? "Active" : "Expired"))
            .ToList();
    }

    /// <summary>A Manager's Course Library visibility: themselves, plus any Student in their
    /// company/companies who is either explicitly assigned to them (StudentProfile.ManagerId) or
    /// not assigned to anyone yet - the exact same "assigned, or falls through to any manager"
    /// rule StudentAuthorization.EnsureCanViewStudentAsync uses. EXISTS-subquery shaped rather
    /// than materializing ids first, since Users/UserCompanyRoles are 100k+ row tables.</summary>
    private IQueryable<string> VisibleEmailsForManagerQuery(int managerId, IReadOnlyCollection<int>? companyIds) =>
        _dbContext.Users.AsNoTracking()
            .Where(u => u.Email != null && (
                u.UserId == managerId
                || (
                    _dbContext.UserCompanyRoles.Any(ucr => ucr.IsActive && ucr.UserId == u.UserId && ucr.Role.RoleName == Roles.Student
                        && (companyIds == null || companyIds.Contains(ucr.CompanyId)))
                    && _dbContext.StudentProfiles.Any(sp => sp.UserId == u.UserId && (sp.ManagerId == managerId || sp.ManagerId == null))
                )))
            .Select(u => u.Email!.ToLower());

    /// <summary>ActiveLibraryCards only has a text Company_Code, not a CompanyId FK - resolve the
    /// selected company scope down to the matching codes once, reused by every query below. Null
    /// means "no restriction"; a non-null (possibly empty) list restricts to exactly those codes.</summary>
    private async Task<List<string>?> ResolveCompanyCodesAsync(IReadOnlyCollection<int>? companyIds, CancellationToken cancellationToken)
    {
        if (companyIds is null)
        {
            return null;
        }

        if (companyIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Companies.AsNoTracking()
            .Where(c => companyIds.Contains(c.CompanyId))
            .Select(c => c.CompanyCode)
            .ToListAsync(cancellationToken);
    }
}
