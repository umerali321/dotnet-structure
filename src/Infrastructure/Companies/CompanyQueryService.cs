using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Companies;

public class CompanyQueryService : ICompanyQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public CompanyQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CompanyListItemDto>> ListAsync(
        IReadOnlyCollection<int>? restrictToCompanyIds,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Companies.AsNoTracking().Where(c => c.IsActive);

        if (restrictToCompanyIds is not null)
        {
            query = query.Where(c => restrictToCompanyIds.Contains(c.CompanyId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(c =>
                EF.Functions.Like(c.CompanyName, term, "\\") || EF.Functions.Like(c.CompanyCode, term, "\\"));
        }

        return await query
            .OrderBy(c => c.CompanyName)
            .Select(c => new CompanyListItemDto(c.CompanyId, c.CompanyCode, c.CompanyName))
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
