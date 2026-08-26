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

/// <summary>Result of a take-course attempt. When the student already completed this course before
/// and hasn't passed ConfirmRetake yet, CourseTaken is null and RequiresConfirmation is true instead
/// of throwing - the client shows ConfirmationMessage in a dialog and resubmits with
/// ConfirmRetake=true to proceed.</summary>
public record TakeCourseResultDto(
    CourseTakenDto? CourseTaken,
    bool RequiresConfirmation,
    string? ConfirmationMessage);
