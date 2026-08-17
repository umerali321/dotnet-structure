using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;

namespace SkillsetsBackend.Infrastructure.CourseLibrary;

public class CourseLibraryQueryService : ICourseLibraryQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public CourseLibraryQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Two queries total (categories, then course id/title for those categories) - name-only, no
    // sections/about content here. Full course detail is loaded separately, on demand, by
    // GetCourseDetailAsync once the user actually opens a course.
    public async Task<IReadOnlyList<CourseLibraryCategoryDto>> GetCategoriesAsync(
        CourseLibraryQueryOptions options, CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.LibraryCategories
            .AsNoTracking()
            .Where(c => c.TypeId == options.TypeId && c.IsActive)
            .OrderBy(c => c.DisplayOrder ?? int.MaxValue)
            .ThenBy(c => c.CategoryName)
            .Select(c => new { c.CategoryId, c.CategoryName })
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
        {
            return [];
        }

        var categoryIds = categories.Select(c => c.CategoryId).ToList();

        var courses = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.CategoryId) && c.IsActive)
            .OrderBy(c => c.DisplayOrder ?? int.MaxValue)
            .ThenBy(c => c.CourseTitle)
            .Select(c => new { c.CourseId, c.CategoryId, c.CourseTitle, c.Duration, c.ExpertiseLevel, c.CourseUrl, c.LaunchUrl })
            .ToListAsync(cancellationToken);

        var courseIds = courses.Select(c => c.CourseId).ToList();
        var contentCounts = await _dbContext.CourseSections
            .AsNoTracking()
            .Where(s => courseIds.Contains(s.CourseId))
            .GroupBy(s => s.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

        var coursesByCategory = courses
            .GroupBy(c => c.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CourseLibraryCourseSummaryDto>)g
                    .Select(c => new CourseLibraryCourseSummaryDto(
                        c.CourseId, c.CourseTitle, c.Duration, c.ExpertiseLevel, c.CourseUrl, c.LaunchUrl,
                        contentCounts.GetValueOrDefault(c.CourseId)))
                    .ToList());

        return categories
            .Select(c => new CourseLibraryCategoryDto(
                c.CategoryId,
                c.CategoryName,
                coursesByCategory.TryGetValue(c.CategoryId, out var categoryCourses) ? categoryCourses : []))
            .ToList();
    }

    public async Task<CourseLibraryCourseDto?> GetCourseDetailAsync(long courseId, CancellationToken cancellationToken = default)
    {
        var course = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.CourseId == courseId && c.IsActive)
            .Join(_dbContext.LibraryCategories.AsNoTracking(), c => c.CategoryId, cat => cat.CategoryId, (c, cat) => new
            {
                c.CourseId,
                c.CourseTitle,
                c.Duration,
                c.ExpertiseLevel,
                c.Status,
                c.CourseUrl,
                c.LaunchUrl,
                c.AboutContent,
                c.OverviewContent,
                c.ImageUrl,
                c.SkillsoftCourseCode,
                cat.CategoryId,
                cat.CategoryName,
                cat.TypeId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (course is null)
        {
            return null;
        }

        var sections = await _dbContext.CourseSections
            .AsNoTracking()
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
            .ThenBy(s => s.SectionId)
            .Select(s => new CourseLibrarySectionDto(s.SectionId, s.SectionName, s.Duration, s.Status, s.DisplayOrder))
            .ToListAsync(cancellationToken);

        return new CourseLibraryCourseDto(
            course.CourseId,
            course.CourseTitle,
            course.Duration,
            course.ExpertiseLevel,
            course.Status,
            course.CourseUrl,
            course.LaunchUrl,
            course.AboutContent,
            course.OverviewContent,
            course.ImageUrl,
            course.SkillsoftCourseCode,
            sections,
            course.CategoryId,
            course.CategoryName,
            course.TypeId);
    }

    // Two queries at most: title matches first (ranked higher), then description matches to fill
    // any remaining slots, excluding courses already found by title - never both scanned if the
    // title match alone already fills the result limit.
    public async Task<IReadOnlyList<CourseSearchResultDto>> SearchAsync(
        string searchTerm, int limit, CancellationToken cancellationToken = default)
    {
        var term = $"%{EscapeLike(searchTerm)}%";

        var titleMatches = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.IsActive && EF.Functions.Like(c.CourseTitle, term, "\\"))
            .Join(_dbContext.LibraryCategories.AsNoTracking(), c => c.CategoryId, cat => cat.CategoryId, (c, cat) => new { c, cat })
            .Where(x => x.cat.IsActive)
            .OrderBy(x => x.c.CourseTitle)
            .Take(limit)
            .Select(x => new CourseSearchResultDto(x.c.CourseId, x.c.CourseTitle, x.cat.CategoryId, x.cat.CategoryName, x.cat.TypeId, "Title"))
            .ToListAsync(cancellationToken);

        var remaining = limit - titleMatches.Count;
        if (remaining <= 0)
        {
            return titleMatches;
        }

        var matchedIds = titleMatches.Select(x => x.CourseId).ToList();

        var descriptionMatches = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.IsActive && !matchedIds.Contains(c.CourseId)
                && ((c.AboutContent != null && EF.Functions.Like(c.AboutContent, term, "\\"))
                    || (c.OverviewContent != null && EF.Functions.Like(c.OverviewContent, term, "\\"))))
            .Join(_dbContext.LibraryCategories.AsNoTracking(), c => c.CategoryId, cat => cat.CategoryId, (c, cat) => new { c, cat })
            .Where(x => x.cat.IsActive)
            .OrderBy(x => x.c.CourseTitle)
            .Take(remaining)
            .Select(x => new CourseSearchResultDto(x.c.CourseId, x.c.CourseTitle, x.cat.CategoryId, x.cat.CategoryName, x.cat.TypeId, "Description"))
            .ToListAsync(cancellationToken);

        return titleMatches.Concat(descriptionMatches).ToList();
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
