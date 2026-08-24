using FluentValidation;

namespace SkillsetsBackend.Application.SkillTrax.Commands.UpdateSkillTrax;

public class UpdateSkillTraxCommandValidator : AbstractValidator<UpdateSkillTraxCommand>
{
    public UpdateSkillTraxCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CourseIds).NotEmpty().WithMessage("Select at least one course title.");
        RuleForEach(x => x.CourseIds).GreaterThan(0);
    }
}
