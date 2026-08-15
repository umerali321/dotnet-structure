using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;

namespace SkillsetsBackend.Application.CourseLibrary.Queries.GetCourseLibraryCourseDetail;

public class GetCourseLibraryCourseDetailQueryHandler
{
    private readonly ICourseLibraryQueryService _queryService;

    public GetCourseLibraryCourseDetailQueryHandler(ICourseLibraryQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<CourseLibraryCourseDto?> Handle(long courseId, CancellationToken cancellationToken) =>
        _queryService.GetCourseDetailAsync(courseId, cancellationToken);
}
