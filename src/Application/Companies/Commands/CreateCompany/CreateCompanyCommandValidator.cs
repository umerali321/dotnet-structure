using FluentValidation;

namespace SkillsetsBackend.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CompanyEmail).MaximumLength(255).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.CompanyEmail));
        RuleFor(x => x.CompanyPhone).MaximumLength(100);

        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.AdminUsername).NotEmpty().MaximumLength(320);
        RuleFor(x => x.AdminPassword).NotEmpty().MaximumLength(500);
    }
}
