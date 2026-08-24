namespace SkillsetsBackend.Application.Scraper.Interfaces;

public interface IScraperCategoryQueryService
{
    Task<IReadOnlyList<string>> ListCategoryNamesAsync(CancellationToken cancellationToken = default);
}
