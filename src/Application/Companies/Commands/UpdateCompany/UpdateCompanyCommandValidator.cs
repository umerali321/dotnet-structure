using FluentValidation;

namespace SkillsetsBackend.Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CompanyEmail).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.CompanyPhone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Street2).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Zip).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PaymentForm).MaximumLength(100);
        RuleFor(x => x.TotalPayment).GreaterThanOrEqualTo(0).When(x => x.TotalPayment.HasValue);
    }
}
