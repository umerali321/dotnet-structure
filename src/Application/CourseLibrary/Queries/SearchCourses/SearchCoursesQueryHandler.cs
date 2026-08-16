using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;

namespace SkillsetsBackend.Application.CourseLibrary.Queries.SearchCourses;

public class SearchCoursesQueryHandler
{
    private const int ResultLimit = 15;

    private readonly ICourseLibraryQueryService _queryService;

    public SearchCoursesQueryHandler(ICourseLibraryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IReadOnlyList<CourseSearchResultDto>> Handle(SearchCoursesQuery query, CancellationToken cancellationToken)
    {
        var term = query.SearchTerm?.Trim() ?? string.Empty;
        if (term.Length < 2)
        {
            return [];
        }

        return await _queryService.SearchAsync(term, ResultLimit, cancellationToken);
    }
}
