using FluentValidation;
using SkillsetsBackend.Domain.Support;

namespace SkillsetsBackend.Application.SupportContacts.Commands.UpdateSupportContact;

public class UpdateSupportContactCommandValidator : AbstractValidator<UpdateSupportContactCommand>
{
    public UpdateSupportContactCommandValidator()
    {
        RuleFor(x => x.ContactType).NotEmpty().Must(SupportContactType.IsValid)
            .WithMessage($"ContactType must be one of: {string.Join(", ", SupportContactType.All)}.");
        RuleFor(x => x.Value).NotEmpty().MaximumLength(500);
    }
}
