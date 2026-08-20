namespace SkillsetsBackend.Application.CourseLibrary.Queries.ListCourseTaken;

public record ListCourseTakenQuery(int Page, int PageSize, string? StudentName = null, string? CourseTitle = null);
