using FluentValidation;

namespace SkillsetsBackend.Application.Assignments.Commands.UpdateAssignment;

public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
{
    public UpdateAssignmentCommandValidator()
    {
        RuleFor(x => x.EmployeeUserIds).NotEmpty().WithMessage("Select at least one employee.");
        RuleForEach(x => x.EmployeeUserIds).GreaterThan(0);

        // Deliberately NO "start date cannot be in the past" rule here, unlike the create validator.
        //
        // An assignment that is already running has a start date in the past by definition. The most
        // common edit - adding employees to training that is under way - resubmits that same
        // unchanged date and was rejected by its own history: "Start date cannot be in the past."
        // The only way through was to move the start date forward, which rewrites when the training
        // actually began and shifts the Early/Late calculation for everyone already on it.
        //
        // The rule still applies on CREATE, where a start date in the past genuinely is a mistake.
    }
}
