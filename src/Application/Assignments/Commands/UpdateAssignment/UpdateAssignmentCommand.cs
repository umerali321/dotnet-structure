namespace SkillsetsBackend.Application.Assignments.Commands.UpdateAssignment;

/// <summary>Edits an existing assignment's employee list, start date, and optionally its titles.
/// CourseIds is null to leave titles unchanged; when provided, the handler only allows the change
/// if no employee has any progress (In Progress or Completed) on the assignment's current titles -
/// otherwise it's rejected and the training itself must be reassigned via cancel + create instead.
/// ConfirmDespiteWarnings is the same two-phase confirm as CreateAssignmentCommand, applied to
/// newly-added employees against whichever title set is in effect after this update.</summary>
public record UpdateAssignmentCommand(
    IReadOnlyList<int> EmployeeUserIds,
    DateOnly StartDate,
    bool ConfirmDespiteWarnings,
    IReadOnlyList<long>? CourseIds = null);
