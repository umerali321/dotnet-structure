using SkillsetsBackend.Application.Common;

namespace SkillsetsBackend.Application.Students.Queries.ListStudents;

public record ListStudentsQuery(
    int Page,
    int PageSize,
    /// <summary>One field and one term - see SearchCriteria. Null means no search.</summary>
    SearchCriteria? Search,
    int? CompanyId,
    string? StudentType,
    bool? IsActive,
    string? SortBy,
    bool SortDescending);
