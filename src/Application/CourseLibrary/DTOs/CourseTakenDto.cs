namespace SkillsetsBackend.Application.CourseLibrary.DTOs;

/// <summary>MatchedIn tells the client whether the title itself matched, or the match was found
/// in the course's description text - "Each result should show where the match came from."</summary>
public record CourseSearchResultDto(
    long CourseId,
    string CourseTitle,
    int CategoryId,
    string CategoryName,
    int TypeId,
    string MatchedIn);

public record CourseTakenDto(
    int CourseTakenId,
    int UserId,
    string StudentName,
    string? StudentEmail,
    long CourseId,
    string CourseTitle,
    string? CategoryName,
    bool IsActive,
    DateTimeOffset AccessedAt,
    DateTimeOffset? CompletedAt,
    string? CourseUrl);
