using FluentValidation;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Auth.Commands.SwitchCompany;

public class SwitchCompanyCommandValidator : AbstractValidator<SwitchCompanyCommand>
{
    public SwitchCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Role)
            .Must(role => role is Roles.Manager or Roles.Student)
            .WithMessage("Role must be Manager or Student.");
    }
}
