using FluentValidation;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
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

        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.AdminUsername).NotEmpty().MaximumLength(320);
        RuleFor(x => x.AdminPassword).NotEmpty().MaximumLength(500);

        RuleFor(x => x.PlanType).NotEmpty().Must(p => p == Company.TrialPlan || p == Company.LicensePlan)
            .WithMessage($"PlanType must be '{Company.TrialPlan}' or '{Company.LicensePlan}'.");

        // Required for BOTH plan types now - Trial's window is caller-chosen (no more fixed
        // 14-day auto-calculation), so it needs the same explicit start/end as License.
        RuleFor(x => x.LicenseStartDate).NotNull().WithMessage("Start date is required.");
        RuleFor(x => x.LicenseEndDate).NotNull().WithMessage("End date is required.");
        RuleFor(x => x.LicenseEndDate)
            .GreaterThan(x => x.LicenseStartDate!.Value)
            .WithMessage("End date must be after the start date.")
            .When(x => x.LicenseStartDate != null && x.LicenseEndDate != null);
    }
}
