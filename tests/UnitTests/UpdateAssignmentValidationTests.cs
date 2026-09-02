using SkillsetsBackend.Application.Assignments.Commands.CreateAssignment;
using SkillsetsBackend.Application.Assignments.Commands.UpdateAssignment;

namespace SkillsetsBackend.UnitTests;

/// <summary>
/// Editing a running assignment - the common case being "add a few more employees to it" - resubmits
/// the start date it already has, which is in the past by definition. The update validator rejected
/// that with "Start date cannot be in the past", so the only way through was to move the start date
/// forward, rewriting when the training actually began and shifting Early/Late for everyone already
/// on it.
/// </summary>
public class UpdateAssignmentValidationTests
{
    private static readonly DateOnly Yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
    private static readonly DateOnly LongPast = new(2026, 8, 24); // the date from the reported failure

    private static UpdateAssignmentCommand Update(DateOnly startDate) =>
        new(EmployeeUserIds: [188967, 203512], StartDate: startDate,
            ConfirmDespiteWarnings: false, CourseIds: [466]);

    [Fact]
    public void An_assignment_that_already_started_can_still_be_edited()
    {
        var result = new UpdateAssignmentCommandValidator().Validate(Update(LongPast));

        Assert.True(result.IsValid,
            "editing a running assignment must not be blocked by its own start date: "
            + string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Yesterdays_start_date_is_accepted_on_update()
    {
        Assert.True(new UpdateAssignmentCommandValidator().Validate(Update(Yesterday)).IsValid);
    }

    [Fact]
    public void Update_still_requires_at_least_one_employee()
    {
        var result = new UpdateAssignmentCommandValidator().Validate(
            new UpdateAssignmentCommand([], Yesterday, false, [466]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("at least one employee"));
    }

    /// <summary>The rule is only dropped for UPDATE. Creating a brand-new assignment that starts in
    /// the past is still a mistake, and must still be caught.</summary>
    [Fact]
    public void Creating_an_assignment_in_the_past_is_still_rejected()
    {
        var command = new CreateAssignmentCommand(
            CompanyId: 1,
            SourceType: "SingleCourse",
            CourseId: 466,
            SkillTraxId: null,
            EmployeeUserIds: [188967],
            StartDate: Yesterday,
            ConfirmDespiteWarnings: false);

        var result = new CreateAssignmentCommandValidator().Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Start date cannot be in the past"));
    }
}
