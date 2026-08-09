using FluentValidation;

namespace SkillsetsBackend.Application.Support.Commands.CreateSupportRequest;

public class CreateSupportRequestCommandValidator : AbstractValidator<CreateSupportRequestCommand>
{
    public CreateSupportRequestCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CompanyId).GreaterThan(0).When(x => x.CompanyId is not null);
    }
}
