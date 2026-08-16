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

    /// <summary>Global course search - title matches first, then description (About/Overview)
    /// matches, each result reporting which one it matched on. Active courses only.</summary>
    Task<IReadOnlyList<CourseSearchResultDto>> SearchAsync(string searchTerm, int limit, CancellationToken cancellationToken = default);
}
