namespace SkillsetsBackend.Application.Assignments.DTOs;

/// <summary>Derived from the existing CourseTaken table (the same data the "Courses Taken" screen
/// already shows), not the blueprint's full usage-driven completion model (test-score thresholds,
/// Skillport integration) - that piece is still out of scope. This is a simpler, already-achievable
/// signal: NotStarted (no CourseTaken row), InProgress (an active row), Completed (row marked
/// complete).</summary>
public enum AssignmentTitleProgressStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
}

/// <summary>
/// Whether the employee began the work on time, measured against the assignment's OWN start date -
/// never against the 30-day session that begins when they happen to start. That distinction is the
/// whole point: the dates are fixed when the assignment is created, so someone who starts three
/// weeks in is Late rather than silently resetting the clock and looking on-track.
///
/// OnTime is deliberately its own value rather than folded into Early: starting on the very first
/// day is exactly what was asked for, and labelling that "Early" would misreport the common case.
/// </summary>
public enum AssignmentStartTiming
{
    NotStarted = 0,
    Early = 1,
    OnTime = 2,
    Late = 3,
}

/// <summary>Status is the enum's ToString() ("NotStarted" | "InProgress" | "Completed"), kept as a
/// plain string rather than relying on default JSON enum serialization (this API has no global
/// JsonStringEnumConverter configured, so an unconverted enum would serialize as a raw integer) -
/// matches the same convention already used for AssignmentDto.SourceType.</summary>
/// <param name="Timing">AssignmentStartTiming's ToString(), same string-not-int reasoning as Status.</param>
/// <param name="StartedOn">The day this employee first opened this title, or null if they never have.</param>
public record AssignmentTitleProgressDto(
    long CourseId,
    string CourseTitle,
    string Status,
    string Timing,
    DateOnly? StartedOn);

/// <param name="Timing">The employee's timing across the whole assignment, from the earliest title
/// they opened. Any single late title makes the assignment Late - starting one course on time
/// doesn't excuse leaving another until after the due window opened.</param>
/// <param name="StartedOn">The day they first opened ANY title on this assignment, or null.</param>
public record AssignmentEmployeeDto(
    int StudentUserId,
    string? FirstName,
    string? LastName,
    string? Email,
    IReadOnlyList<AssignmentTitleProgressDto> TitleProgress,
    string Timing,
    DateOnly? StartedOn);

public record AssignmentTitleDto(long CourseId, string CourseTitle, string? CourseUrl, string? LaunchUrl);

public record AssignmentDto(
    int AssignmentId,
    int CompanyId,
    string SourceType,
    int? SourceSkillTraxId,
    string? SourceSkillTraxName,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCancelled,
    DateTimeOffset? CancelledAt,
    DateTimeOffset CreatedAt,
    int CreatedByUserId,
    string? CreatedByName,
    string? CreatedByEmail,
    string? UpdatedByName,
    string? UpdatedByEmail,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<AssignmentEmployeeDto> Employees,
    IReadOnlyList<AssignmentTitleDto> Titles);

/// <summary>One employee+course pair that already has a non-cancelled, not-yet-ended assignment -
/// surfaced as a warning before confirming a new one (blueprint #13), never a hard block.</summary>
public record AssignmentOverlapDto(int StudentUserId, string StudentName, long CourseId, string CourseTitle);

public record CreateAssignmentResultDto(AssignmentDto? Assignment, IReadOnlyList<string> Warnings);
