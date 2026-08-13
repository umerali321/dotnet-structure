using FluentValidation;

namespace SkillsetsBackend.Application.Managers.Commands.ProvisionManagerSkillport;

public class ProvisionManagerSkillportCommandValidator : AbstractValidator<ProvisionManagerSkillportCommand>
{
    public ProvisionManagerSkillportCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(50);
    }
}
