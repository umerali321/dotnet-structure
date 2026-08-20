namespace SkillsetsBackend.Application.Dashboard.Dtos;

/// <summary>One ActiveLibraryCards row for a user - their Course Library session history.</summary>
public record CourseLibrarySessionDto(DateTime StartDate, DateTime EndDate, string Status);
