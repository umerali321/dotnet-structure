using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Infrastructure.Skillsoft;
using SkillsetsBackend.Shared.Common;
namespace SkillsetsBackend.Infrastructure.Managers;

public sealed class ManagerQueryService(ApplicationDbContext db) : IManagerQueryService
{
    public async Task<PaginatedList<ManagerListItemDto>> ListAsync(ManagerListQueryOptions o, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Any explicit role name filters to exactly that role; null keeps the historical default of
        // Manager/Admin together. Previously only "CompanyAdmin" was understood and anything else
        // silently fell back to Manager/Admin - which would have listed the wrong people entirely
        // for a new role such as SystemAdmin.
        var roleNames = string.IsNullOrWhiteSpace(o.RoleFilter)
            ? new[] { "Manager", "Admin" }
            : new[] { o.RoleFilter };
        // Deliberately NOT filtered by x.Company.IsActive - a company going inactive blocks its
        // users from logging in (enforced separately in UserDirectory.QueryActiveCompanyRoles,
        // which backs login/company-selection), it must not also hide their records from admin
        // listings/search. x.IsActive is a different flag (this specific role assignment's own
        // active state) and stays.
        var memberships = db.UserCompanyRoles.AsNoTracking()
            .Where(x => x.IsActive && roleNames.Contains(x.Role.RoleName) && (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today));

        var query = db.Users.AsNoTracking().Where(u => memberships.Any(x => x.UserId == u.UserId));

        if (o.RestrictToCompanyIds is not null)
        {
            query = query.Where(u => memberships.Any(x => x.UserId == u.UserId && o.RestrictToCompanyIds.Contains(x.CompanyId)));
        }

        if (o.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == o.IsActive);
        }

        // One named column, prefix first - see SearchCriteria. The previous form OR'd '%term%'
        // across six expressions and could not use IX_Users_Email or IX_Users_FirstName_LastName
        // at all.
        var unsearched = query;

        if (o.Search is { } search)
        {
            query = ApplySearch(unsearched, memberships, search.Field, search.ToPrefixPattern());
        }

        var total = await query.CountAsync(ct);

        // Nothing matched as a prefix - retry once with contains, so a mid-string search still
        // works. Only reached when the fast path came back empty.
        if (total == 0 && o.Search is { } fallback)
        {
            query = ApplySearch(unsearched, memberships, fallback.Field, fallback.ToContainsPattern());
            total = await query.CountAsync(ct);
        }


        var ordered = o.SortBy?.ToLowerInvariant() switch
        {
            "firstname" => o.SortDescending ? query.OrderByDescending(x => x.FirstName).ThenBy(x => x.UserId) : query.OrderBy(x => x.FirstName).ThenBy(x => x.UserId),
            "email" => o.SortDescending ? query.OrderByDescending(x => x.Email).ThenBy(x => x.UserId) : query.OrderBy(x => x.Email).ThenBy(x => x.UserId),
            // Newest-added OR most-recently-edited first - UpdatedAt bumps on a name/email/phone
            // edit or activate/deactivate (see AppUser.cs).
            "recent" => query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ThenByDescending(x => x.UserId),
            _ => query.OrderBy(x => x.UserId)
        };

        var page = await ordered.Skip((o.Page - 1) * o.PageSize).Take(o.PageSize).ToListAsync(ct);
        var ids = page.Select(x => x.UserId).ToList();

        var managerMemberships = await memberships
            .Where(x => ids.Contains(x.UserId))
            .Select(x => new { x.UserId, x.CompanyId, x.Company.CompanyName, x.Company.CompanyCode, x.Role.RoleName, x.StartDate, x.EndDate })
            .ToListAsync(ct);

        var map = managerMemberships
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ManagerCompanyDto>)g.Select(x => new ManagerCompanyDto(x.CompanyId, x.CompanyCode, x.CompanyName, Roles.Normalize(x.RoleName), x.StartDate, x.EndDate)).ToList());

        var codesByUser = managerMemberships
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CompanyCode).ToList());

        var activePairs = await ActiveLibraryCardLookup.GetActivePairsAsync(db, codesByUser.Values.SelectMany(codes => codes), ct);

        var items = page.Select(u => new ManagerListItemDto(
            u.UserId,
            u.FirstName,
            u.LastName,
            u.Email,
            u.Username,
            u.Phone,
            u.IsActive,
            u.CreatedAt,
            map.GetValueOrDefault(u.UserId, []),
            HasActiveSkillportCard(u.UserId, u.Email, codesByUser, activePairs))).ToList();

        return new PaginatedList<ManagerListItemDto>(items, total, o.Page, o.PageSize);
    }

    public async Task<ManagerListItemDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Unlike ListAsync, this is looked up by a specific UserId (e.g. a detail page, or a
        // CompanyAdmin viewing their own profile) - widened to include CompanyAdmin unconditionally
        // rather than gated by RoleFilter, since the caller already knows which user they want.
        // Not filtered by x.Company.IsActive - see the identical note in ListAsync above. The detail
        // page must keep showing which company this person belongs to even once that company is
        // deactivated, not blank it out.
        var memberships = db.UserCompanyRoles.AsNoTracking()
            .Where(x => x.UserId == id && x.IsActive
                && (x.Role.RoleName == "Manager" || x.Role.RoleName == "Admin" || x.Role.RoleName == "CompanyAdmin")
                && (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today));

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id, ct);
        if (user is null)
        {
            return null;
        }

        var companyRows = await memberships
            .Select(x => new { x.CompanyId, x.Company.CompanyName, x.Company.CompanyCode, x.Role.RoleName, x.StartDate, x.EndDate })
            .ToListAsync(ct);

        var companies = companyRows.Select(x => new ManagerCompanyDto(x.CompanyId, x.CompanyCode, x.CompanyName, Roles.Normalize(x.RoleName), x.StartDate, x.EndDate)).ToList();
        var codesByUser = new Dictionary<int, List<string>> { [id] = companyRows.Select(x => x.CompanyCode).ToList() };
        var activePairs = await ActiveLibraryCardLookup.GetActivePairsAsync(db, codesByUser[id], ct);

        return new ManagerListItemDto(
            user.UserId, user.FirstName, user.LastName, user.Email, user.Username, user.Phone, user.IsActive, user.CreatedAt,
            companies, HasActiveSkillportCard(id, user.Email, codesByUser, activePairs));
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

    /// <summary>Narrows to ONE field. Written once so the prefix attempt and the contains fallback
    /// are the same predicate with a different pattern.</summary>
    private static IQueryable<AppUser> ApplySearch(
        IQueryable<AppUser> query,
        IQueryable<UserCompanyRole> memberships,
        SearchBy field,
        string term) => field switch
    {
        SearchBy.Name => query.Where(u =>
            EF.Functions.Like(u.FirstName!, term, "\\") ||
            EF.Functions.Like(u.LastName!, term, "\\") ||
            EF.Functions.Like((u.FirstName ?? "") + " " + (u.LastName ?? ""), term, "\\") ||
            EF.Functions.Like((u.LastName ?? "") + " " + (u.FirstName ?? ""), term, "\\")),

        SearchBy.Email => query.Where(u =>
            EF.Functions.Like(u.Email!, term, "\\") ||
            EF.Functions.Like(u.Username!, term, "\\")),

        SearchBy.Company => query.Where(u => memberships.Any(x =>
            x.UserId == u.UserId &&
            (EF.Functions.Like(x.Company.CompanyName!, term, "\\") ||
             EF.Functions.Like(x.Company.CompanyCode!, term, "\\")))),

        SearchBy.Phone => query.Where(u => EF.Functions.Like(u.Phone!, term, "\\")),

        // Narrow to nothing rather than silently returning the whole list unfiltered.
        _ => query.Where(_ => false),
    };

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
