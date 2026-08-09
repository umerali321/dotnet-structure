using FluentValidation;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.SupportContacts.Commands.CreateSupportContact;

public class CreateSupportContactCommandValidator : AbstractValidator<CreateSupportContactCommand>
{
    public CreateSupportContactCommandValidator()
    {
        RuleFor(x => x.ContactType).NotEmpty().Must(SupportContactType.IsValid)
            .WithMessage($"ContactType must be one of: {string.Join(", ", SupportContactType.All)}.");
        RuleFor(x => x.Value).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CompanyId).GreaterThan(0).When(x => x.CompanyId is not null);
    }
}
