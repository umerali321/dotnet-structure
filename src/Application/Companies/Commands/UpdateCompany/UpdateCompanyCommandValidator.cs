using FluentValidation;

namespace SkillsetsBackend.Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.CompanyEmail).MaximumLength(255).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.CompanyEmail));
        RuleFor(x => x.CompanyPhone).MaximumLength(100);
        RuleFor(x => x.Street1).MaximumLength(255);
        RuleFor(x => x.Street2).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.Zip).MaximumLength(20);
        RuleFor(x => x.PaymentForm).MaximumLength(100);
        RuleFor(x => x.TotalPayment).GreaterThanOrEqualTo(0).When(x => x.TotalPayment.HasValue);
    }
}
