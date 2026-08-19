using FluentValidation;

namespace SkillsetsBackend.Application.Companies.Commands.SetCompanyLicense;

public class SetCompanyLicenseCommandValidator : AbstractValidator<SetCompanyLicenseCommand>
{
    public SetCompanyLicenseCommandValidator()
    {
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("License end date must be after the start date.");
    }
}
