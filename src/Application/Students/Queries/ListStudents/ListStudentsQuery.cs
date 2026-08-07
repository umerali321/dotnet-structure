namespace SkillsetsBackend.Application.Students.Queries.ListStudents;

public record ListStudentsQuery(
    int Page,
    int PageSize,
    string? Search,
    int? CompanyId,
    string? StudentType,
    bool? IsActive,
    string? SortBy,
    bool SortDescending);
