using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Dashboard.Dtos;
using SkillsetsBackend.Application.Dashboard.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.Dashboard;

/// <summary>
/// Every count here is built as a server-translatable IQueryable (COUNT DISTINCT / EXISTS-subquery
/// shapes) rather than materializing user-id lists into memory first - the Users/UserCompanyRoles
/// tables are large enough (100k+ rows) that a client-evaluated ".Contains(bigList)" would blow past
/// SQL Server's ~2100 parameter limit. ActiveLibraryCards is the one exception - it's a small, fixed
/// ~4.5k-row table, so pulling a filtered slice into memory to group by (Email, CompanyCode) is safe
/// and simpler than fighting EF's grouped-aggregate SQL translation.
/// </summary>
public class DashboardQueryService : IDashboardQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(
        IReadOnlyCollection<int>? companyIds,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ---- Company snapshot buckets - reuses the exact Trial/License/Inactive predicate from
        // CompanyQueryService/sp_ListCompanies. Never date-filtered (see DashboardStatsDto). ----
        var companiesQuery = _dbContext.Companies.AsNoTracking();
        if (companyIds is not null)
        {
            companiesQuery = companiesQuery.Where(c => companyIds.Contains(c.CompanyId));
        }

        var totalCompanies = await companiesQuery.CountAsync(cancellationToken);
        var trialCompanies = await companiesQuery.CountAsync(
            c => c.IsActive && c.PlanEndDate >= today && c.PlanType == Company.TrialPlan, cancellationToken);
        var licensedCompanies = await companiesQuery.CountAsync(
            c => c.IsActive && c.PlanEndDate >= today && c.PlanType == Company.LicensePlan, cancellationToken);
        var inactiveCompanies = await companiesQuery.CountAsync(
            c => !c.IsActive || c.PlanEndDate < today, cancellationToken);

        // ---- Headcounts - active UserCompanyRoles by role, not gated on Company.IsActive (a
        // headcount should still count a company's people even if its license just lapsed). ----
        var totalCompanyAdmins = await ActiveRoleUserIdsQuery(companyIds, [Roles.CompanyAdmin], today).CountAsync(cancellationToken);
        var totalManagers = await ActiveRoleUserIdsQuery(companyIds, ManagerRoleNames, today).CountAsync(cancellationToken);
        var employeeIdsQuery = ActiveRoleUserIdsQuery(companyIds, [Roles.Student], today);
        var totalEmployees = await employeeIdsQuery.CountAsync(cancellationToken);

        // ---- IT / NON-IT split of that same active-employee set. Real data has ~10 distinct
        // StudentType values (mostly "IT"/"NON" plus a handful of legacy stragglers) - bucket as
        // exact "IT" vs everything else so IT + NON-IT always equals TotalEmployees exactly. ----
        var itEmployees = await _dbContext.StudentProfiles.AsNoTracking()
            .Where(sp => sp.StudentType == "IT" && employeeIdsQuery.Contains(sp.UserId))
            .CountAsync(cancellationToken);
        var nonItEmployees = totalEmployees - itEmployees;

        // ---- Course Library (ActiveLibraryCards) - distinct (Email, CompanyCode) pairs. ----
        var companyCodes = await ResolveCompanyCodesAsync(companyIds, cancellationToken);
        var cardsQuery = _dbContext.ActiveLibraryCards.AsNoTracking().Where(c => c.Email != null);
        if (companyCodes is not null)
        {
            cardsQuery = cardsQuery.Where(c => companyCodes.Contains(c.CompanyCode));
        }

        var courseLibraryUsers = await cardsQuery
            .Select(c => new { Email = c.Email!.ToLower(), c.CompanyCode })
            .Distinct()
            .CountAsync(cancellationToken);

        // ---- Date-range "activity in period" numbers - the only fields the date filter changes. ----
        var companiesAddedInPeriod = 0;
        var usersAddedInPeriod = 0;
        var sessionsStartedInPeriod = 0;

        if (startDate.HasValue || endDate.HasValue)
        {
            DateTimeOffset? rangeStart = startDate.HasValue
                ? new DateTimeOffset(startDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : null;
            DateTimeOffset? rangeEnd = endDate.HasValue
                ? new DateTimeOffset(endDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero)
                : null;

            companiesAddedInPeriod = await companiesQuery.CountAsync(
                c => (rangeStart == null || c.CreatedAt >= rangeStart) && (rangeEnd == null || c.CreatedAt <= rangeEnd),
                cancellationToken);

            var scopedPersonIdsQuery = _dbContext.UserCompanyRoles.AsNoTracking()
                .Where(ucr => ucr.IsActive
                    && (ucr.Role.RoleName == Roles.CompanyAdmin || ucr.Role.RoleName == Roles.Manager
                        || ucr.Role.RoleName == LegacyAdminRoleName || ucr.Role.RoleName == Roles.Student)
                    && (ucr.StartDate == null || ucr.StartDate <= today)
                    && (ucr.EndDate == null || ucr.EndDate >= today)
                    && (companyIds == null || companyIds.Contains(ucr.CompanyId)))
                .Select(ucr => ucr.UserId);

            usersAddedInPeriod = await _dbContext.Users.AsNoTracking()
                .Where(u => scopedPersonIdsQuery.Contains(u.UserId)
                    && (rangeStart == null || u.CreatedAt >= rangeStart) && (rangeEnd == null || u.CreatedAt <= rangeEnd))
                .CountAsync(cancellationToken);

            DateTime? cardRangeStart = startDate?.ToDateTime(TimeOnly.MinValue);
            DateTime? cardRangeEnd = endDate?.ToDateTime(TimeOnly.MaxValue);
            sessionsStartedInPeriod = await cardsQuery.CountAsync(
                c => (cardRangeStart == null || c.StartDate >= cardRangeStart) && (cardRangeEnd == null || c.StartDate <= cardRangeEnd),
                cancellationToken);
        }

        return new DashboardStatsDto(
            totalCompanies, totalCompanyAdmins, totalManagers, totalEmployees,
            trialCompanies, licensedCompanies, inactiveCompanies,
            itEmployees, nonItEmployees, courseLibraryUsers,
            companiesAddedInPeriod, usersAddedInPeriod, sessionsStartedInPeriod);
    }

    public async Task<PaginatedList<CourseLibraryUserDto>> GetCourseLibraryUsersAsync(
        IReadOnlyCollection<int>? companyIds,
        DateOnly? startDate,
        DateOnly? endDate,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var companyCodes = await ResolveCompanyCodesAsync(companyIds, cancellationToken);

        var cardsQuery = _dbContext.ActiveLibraryCards.AsNoTracking().Where(c => c.Email != null);
        if (companyCodes is not null)
        {
            cardsQuery = cardsQuery.Where(c => companyCodes.Contains(c.CompanyCode));
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
            .Select(u => new { u.UserId, Email = u.Email!.ToLower(), u.FirstName, u.LastName })
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
                studentType, g.LatestStart, g.LatestEnd, status, g.SessionCount);
        }).ToList();

        return new PaginatedList<CourseLibraryUserDto>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<CourseLibrarySessionDto>> GetSessionHistoryAsync(
        string email,
        IReadOnlyCollection<int>? companyIds,
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

        var rows = await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new { c.StartDate, c.EndDate })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return rows
            .Select(r => new CourseLibrarySessionDto(r.StartDate, r.EndDate, now >= r.StartDate && now <= r.EndDate ? "Active" : "Expired"))
            .ToList();
    }

    /// <summary>The DB still has a distinct legacy "Admin" Roles row, separate from "Manager" -
    /// Roles.Normalize() folds it into Manager everywhere else in the app (see Roles.cs), so the
    /// Manager headcount here must match both names too, not just an exact "Manager".</summary>
    private const string LegacyAdminRoleName = "Admin";
    private static readonly string[] ManagerRoleNames = [Roles.Manager, LegacyAdminRoleName];

    private IQueryable<int> ActiveRoleUserIdsQuery(IReadOnlyCollection<int>? companyIds, IReadOnlyCollection<string> roleNames, DateOnly today) =>
        _dbContext.UserCompanyRoles.AsNoTracking()
            .Where(ucr => ucr.IsActive && roleNames.Contains(ucr.Role.RoleName)
                && (ucr.StartDate == null || ucr.StartDate <= today)
                && (ucr.EndDate == null || ucr.EndDate >= today)
                && (companyIds == null || companyIds.Contains(ucr.CompanyId)))
            .Select(ucr => ucr.UserId)
            .Distinct();

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
