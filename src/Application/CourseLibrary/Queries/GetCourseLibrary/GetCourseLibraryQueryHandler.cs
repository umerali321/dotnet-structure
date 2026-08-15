using FluentValidation.Results;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.CourseLibrary.DTOs;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;

namespace SkillsetsBackend.Application.CourseLibrary.Queries.GetCourseLibrary;

public class GetCourseLibraryQueryHandler
{
    private readonly ICourseLibraryQueryService _queryService;

    public GetCourseLibraryQueryHandler(ICourseLibraryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<CourseLibraryResponseDto> Handle(GetCourseLibraryQuery query, CancellationToken cancellationToken)
    {
        var (typeId, label) = MapType(query.Type);

        var categories = await _queryService.GetCategoriesAsync(new CourseLibraryQueryOptions(typeId), cancellationToken);

        return new CourseLibraryResponseDto(label, categories);
    }

    // TypeID meaning (LibraryCategories.TypeID) is a data fact, not something to hard-code as a
    // magic number scattered across the codebase - this is the single place the client-facing
    // "non-it"/"it" tab value is translated to it.
    private static (int TypeId, string Label) MapType(string type) => type?.Trim().ToLowerInvariant() switch
    {
        "non-it" => (1, "NON_IT"),
        "it" => (2, "IT"),
        _ => throw new ValidationException(
            [new ValidationFailure("type", "type must be 'non-it' or 'it'.")]),
    };
}
