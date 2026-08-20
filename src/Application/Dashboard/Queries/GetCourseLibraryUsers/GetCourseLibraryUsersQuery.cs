namespace SkillsetsBackend.Application.Dashboard.Queries.GetCourseLibraryUsers;

public record GetCourseLibraryUsersQuery(
    int? CompanyId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Search,
    int Page,
    int PageSize);
