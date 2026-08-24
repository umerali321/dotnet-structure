using FluentValidation;

namespace SkillsetsBackend.Application.Assignments.Commands.UpdateAssignment;

public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
{
    public UpdateAssignmentCommandValidator()
    {
        RuleFor(x => x.EmployeeUserIds).NotEmpty().WithMessage("Select at least one employee.");
        RuleForEach(x => x.EmployeeUserIds).GreaterThan(0);

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date cannot be in the past.");
    }
}
