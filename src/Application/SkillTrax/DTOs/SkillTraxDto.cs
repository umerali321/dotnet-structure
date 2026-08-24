namespace SkillsetsBackend.Application.SkillTrax.DTOs;

public record SkillTraxCourseDto(long CourseId, string CourseTitle, string? Duration);

/// <summary>Which assignment currently uses this SkillTrax, and who it targets - lets the SkillTrax
/// detail view show "who has this assigned" without a separate round trip.</summary>
public record SkillTraxAssignmentUsageDto(
    int AssignmentId, bool IsCancelled, DateOnly StartDate, DateOnly EndDate, IReadOnlyList<string> EmployeeNames);

public record SkillTraxDto(
    int SkillTraxId,
    string Name,
    int CompanyId,
    string? CompanyName,
    int CreatedByUserId,
    string? CreatedByName,
    string? CreatedByEmail,
    DateTimeOffset CreatedAt,
    string? UpdatedByName,
    string? UpdatedByEmail,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<SkillTraxCourseDto> Courses,
    IReadOnlyList<SkillTraxAssignmentUsageDto> Assignments);

public record SkillTraxSummaryDto(
    int SkillTraxId,
    string Name,
    int CompanyId,
    string? CompanyName,
    DateTimeOffset CreatedAt,
    int CourseCount,
    /// <summary>Distinct employees currently covered by a non-cancelled assignment sourced from
    /// this SkillTrax - "how many people is this bundle assigned to right now."</summary>
    int AssignedMemberCount,
    string? CreatedByName,
    string? CreatedByEmail,
    string? UpdatedByName,
    string? UpdatedByEmail,
    DateTimeOffset? UpdatedAt);
