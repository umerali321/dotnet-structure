using FluentValidation;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
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

        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.AdminUsername).NotEmpty().MaximumLength(320);
        RuleFor(x => x.AdminPassword).NotEmpty().MaximumLength(500);

        RuleFor(x => x.PlanType).NotEmpty().Must(p => p == Company.TrialPlan || p == Company.LicensePlan)
            .WithMessage($"PlanType must be '{Company.TrialPlan}' or '{Company.LicensePlan}'.");

        RuleFor(x => x.LicenseStartDate).NotNull().WithMessage("License start date is required.")
            .When(x => x.PlanType == Company.LicensePlan);
        RuleFor(x => x.LicenseEndDate).NotNull().WithMessage("License end date is required.")
            .When(x => x.PlanType == Company.LicensePlan);
        RuleFor(x => x.LicenseEndDate)
            .GreaterThan(x => x.LicenseStartDate!.Value)
            .WithMessage("License end date must be after the start date.")
            .When(x => x.PlanType == Company.LicensePlan && x.LicenseStartDate != null && x.LicenseEndDate != null);
    }
}
