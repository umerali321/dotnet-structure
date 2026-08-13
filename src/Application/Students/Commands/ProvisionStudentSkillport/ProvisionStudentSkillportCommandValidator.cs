using FluentValidation;

namespace SkillsetsBackend.Application.Students.Commands.ProvisionStudentSkillport;

public class ProvisionStudentSkillportCommandValidator : AbstractValidator<ProvisionStudentSkillportCommand>
{
    public ProvisionStudentSkillportCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(50);
    }
}
