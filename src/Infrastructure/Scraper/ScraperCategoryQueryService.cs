using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.Scraper.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.Scraper;

public class ScraperCategoryQueryService : IScraperCategoryQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public ScraperCategoryQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<string>> ListCategoryNamesAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.MainCourseCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.TypeId).ThenBy(c => c.DisplayOrder ?? int.MaxValue).ThenBy(c => c.CategoryName)
            .Select(c => c.CategoryName)
            .ToListAsync(cancellationToken);
}
