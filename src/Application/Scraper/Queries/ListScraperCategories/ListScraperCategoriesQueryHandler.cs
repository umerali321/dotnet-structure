using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Scraper.Interfaces;

namespace SkillsetsBackend.Application.Scraper.Queries.ListScraperCategories;

public class ListScraperCategoriesQueryHandler
{
    private readonly IScraperCategoryQueryService _categoryService;

    public ListScraperCategoriesQueryHandler(IScraperCategoryQueryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IReadOnlyList<string>> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view the course scraper categories.");
        }

        var names = await _categoryService.ListCategoryNamesAsync(cancellationToken);
        return new[] { "ALL" }.Concat(names).ToList();
    }
}
