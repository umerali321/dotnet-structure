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

/// <summary>Status is the enum's ToString() ("NotStarted" | "InProgress" | "Completed"), kept as a
/// plain string rather than relying on default JSON enum serialization (this API has no global
/// JsonStringEnumConverter configured, so an unconverted enum would serialize as a raw integer) -
/// matches the same convention already used for AssignmentDto.SourceType.</summary>
public record AssignmentTitleProgressDto(long CourseId, string CourseTitle, string Status);

public record AssignmentEmployeeDto(
    int StudentUserId,
    string? FirstName,
    string? LastName,
    string? Email,
    IReadOnlyList<AssignmentTitleProgressDto> TitleProgress);

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
    string? CreatedByEmail,
    IReadOnlyList<AssignmentEmployeeDto> Employees,
    IReadOnlyList<AssignmentTitleDto> Titles);

/// <summary>One employee+course pair that already has a non-cancelled, not-yet-ended assignment -
/// surfaced as a warning before confirming a new one (blueprint #13), never a hard block.</summary>
public record AssignmentOverlapDto(int StudentUserId, string StudentName, long CourseId, string CourseTitle);

public record CreateAssignmentResultDto(AssignmentDto? Assignment, IReadOnlyList<string> Warnings);
