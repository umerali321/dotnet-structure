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
    string? CourseUrl,
    /// <summary>The most recently created non-cancelled Assignment's StartDate that targeted this
    /// employee with this course - i.e. when the course was assigned/due, shown alongside AccessedAt
    /// so Status is easy to verify against both dates. Null when this course wasn't part of any
    /// assignment for this employee (a self-initiated course has no such date).</summary>
    DateOnly? CourseDate = null,
    /// <summary>AssignmentStartTiming's ToString() ("Early"/"OnTime"/"Late"), same string-not-int
    /// reasoning as AssignmentTitleProgressDto.Timing (this API has no global JsonStringEnumConverter,
    /// so an unconverted enum would serialize as a raw integer). Null when this course wasn't part of
    /// any (non-cancelled) Assignment for this employee - status only has meaning relative to an
    /// assignment's own start date, so a self-initiated course (no assignment) has none. When set,
    /// compares this row's own AccessedAt against CourseDate above - see AssignmentTiming.</summary>
    string? Status = null);

/// <summary>Result of a take-course attempt. When the student already completed this course before
/// and hasn't passed ConfirmRetake yet, CourseTaken is null and RequiresConfirmation is true instead
/// of throwing - the client shows ConfirmationMessage in a dialog and resubmits with
/// ConfirmRetake=true to proceed.</summary>
public record TakeCourseResultDto(
    CourseTakenDto? CourseTaken,
    bool RequiresConfirmation,
    string? ConfirmationMessage);
