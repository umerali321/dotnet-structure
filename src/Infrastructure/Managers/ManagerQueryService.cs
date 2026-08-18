using Microsoft.EntityFrameworkCore;
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
        var roleNames = o.RoleFilter == "CompanyAdmin" ? new[] { "CompanyAdmin" } : new[] { "Manager", "Admin" };
        var memberships = db.UserCompanyRoles.AsNoTracking()
            .Where(x => x.IsActive && x.Company.IsActive && roleNames.Contains(x.Role.RoleName) && (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today));

        var query = db.Users.AsNoTracking().Where(u => memberships.Any(x => x.UserId == u.UserId));

        if (o.RestrictToCompanyIds is not null)
        {
            query = query.Where(u => memberships.Any(x => x.UserId == u.UserId && o.RestrictToCompanyIds.Contains(x.CompanyId)));
        }

        if (o.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == o.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(o.Search))
        {
            var term = $"%{o.Search.Trim()}%";
            query = query.Where(u => EF.Functions.Like(u.FirstName!, term) || EF.Functions.Like(u.LastName!, term) || EF.Functions.Like(u.Email!, term) || EF.Functions.Like(u.Username!, term));
        }

        var total = await query.CountAsync(ct);

        var ordered = o.SortBy?.ToLowerInvariant() switch
        {
            "firstname" => o.SortDescending ? query.OrderByDescending(x => x.FirstName).ThenBy(x => x.UserId) : query.OrderBy(x => x.FirstName).ThenBy(x => x.UserId),
            "email" => o.SortDescending ? query.OrderByDescending(x => x.Email).ThenBy(x => x.UserId) : query.OrderBy(x => x.Email).ThenBy(x => x.UserId),
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
        var memberships = db.UserCompanyRoles.AsNoTracking()
            .Where(x => x.UserId == id && x.IsActive && x.Company.IsActive
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
}
