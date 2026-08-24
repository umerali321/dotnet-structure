using FluentValidation;

namespace SkillsetsBackend.Application.SkillTrax.Commands.CreateSkillTrax;

public class CreateSkillTraxCommandValidator : AbstractValidator<CreateSkillTraxCommand>
{
    public CreateSkillTraxCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CourseIds).NotEmpty().WithMessage("Select at least one course title.");
        RuleForEach(x => x.CourseIds).GreaterThan(0);
    }
}
