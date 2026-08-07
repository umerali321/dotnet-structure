using FluentValidation;

namespace SkillsetsBackend.Application.Auth.Commands.SwitchCompany;

public class SwitchCompanyCommandValidator : AbstractValidator<SwitchCompanyCommand>
{
    public SwitchCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}
