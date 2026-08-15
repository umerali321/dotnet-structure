using SkillsetsBackend.Application.CourseLibrary.DTOs;

namespace SkillsetsBackend.Application.CourseLibrary.Interfaces;

public record CourseLibraryQueryOptions(int TypeId);

public interface ICourseLibraryQueryService
{
    Task<IReadOnlyList<CourseLibraryCategoryDto>> GetCategoriesAsync(
        CourseLibraryQueryOptions options, CancellationToken cancellationToken = default);

    /// <summary>Full detail (About content + Table of Contents) for one course, fetched lazily
    /// only when the user opens it. Null if the course doesn't exist or isn't active.</summary>
    Task<CourseLibraryCourseDto?> GetCourseDetailAsync(long courseId, CancellationToken cancellationToken = default);
}
