using FluentValidation;

namespace SkillsetsBackend.Application.Auth.Commands.SubmitLoginSupportRequest;

public class SubmitLoginSupportRequestCommandValidator : AbstractValidator<SubmitLoginSupportRequestCommand>
{
    public SubmitLoginSupportRequestCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.CompanyName).MaximumLength(200);
        RuleFor(x => x.Message).MaximumLength(2000);
    }
}
