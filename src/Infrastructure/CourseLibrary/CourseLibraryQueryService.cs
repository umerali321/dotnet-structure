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
            .Select(c => new { c.CourseId, c.CategoryId, c.CourseTitle, c.Duration, c.ExpertiseLevel, c.LaunchUrl })
            .ToListAsync(cancellationToken);

        var coursesByCategory = courses
            .GroupBy(c => c.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CourseLibraryCourseSummaryDto>)g
                    .Select(c => new CourseLibraryCourseSummaryDto(c.CourseId, c.CourseTitle, c.Duration, c.ExpertiseLevel, c.LaunchUrl))
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
            .Select(c => new
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
            sections);
    }
}
