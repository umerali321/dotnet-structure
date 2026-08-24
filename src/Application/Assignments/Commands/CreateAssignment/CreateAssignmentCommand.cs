namespace SkillsetsBackend.Application.Assignments.Commands.CreateAssignment;

/// <summary>SourceType is "SingleCourse" (CourseId set, SkillTraxId null) or "SkillTrax"
/// (SkillTraxId set, CourseId null). ConfirmDespiteWarnings is the two-phase confirm for the
/// duplicate/overlap check (blueprint #13): first submit with it false surfaces warnings without
/// creating anything; resubmitting with it true proceeds anyway.</summary>
/// <summary>ActingAsUserId is required only when the caller is SuperAdmin (a real Manager/Company
/// Admin at CompanyId to create this on behalf of - see ActingAsResolver); ignored otherwise.</summary>
public record CreateAssignmentCommand(
    int CompanyId,
    string SourceType,
    long? CourseId,
    int? SkillTraxId,
    IReadOnlyList<int> EmployeeUserIds,
    DateOnly StartDate,
    bool ConfirmDespiteWarnings,
    int? ActingAsUserId = null);
