using FluentValidation;
using SkillsetsBackend.Domain.Assignments;

namespace SkillsetsBackend.Application.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);

        RuleFor(x => x.SourceType)
            .Must(t => Enum.TryParse<AssignmentSourceType>(t, out _))
            .WithMessage("SourceType must be 'SingleCourse' or 'SkillTrax'.");

        RuleFor(x => x.CourseId).NotNull().GreaterThan(0)
            .When(x => x.SourceType == nameof(AssignmentSourceType.SingleCourse))
            .WithMessage("CourseId is required for a single-course assignment.");
        RuleFor(x => x.SkillTraxId).Null()
            .When(x => x.SourceType == nameof(AssignmentSourceType.SingleCourse))
            .WithMessage("SkillTraxId must not be set for a single-course assignment.");

        RuleFor(x => x.SkillTraxId).NotNull().GreaterThan(0)
            .When(x => x.SourceType == nameof(AssignmentSourceType.SkillTrax))
            .WithMessage("SkillTraxId is required for a SkillTrax assignment.");
        RuleFor(x => x.CourseId).Null()
            .When(x => x.SourceType == nameof(AssignmentSourceType.SkillTrax))
            .WithMessage("CourseId must not be set for a SkillTrax assignment.");

        RuleFor(x => x.EmployeeUserIds).NotEmpty().WithMessage("Select at least one employee.");
        RuleForEach(x => x.EmployeeUserIds).GreaterThan(0);

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date cannot be in the past.");
    }
}
