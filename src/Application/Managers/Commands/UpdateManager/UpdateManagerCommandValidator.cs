using FluentValidation;

namespace SkillsetsBackend.Application.Managers.Commands.UpdateManager;

public class UpdateManagerCommandValidator : AbstractValidator<UpdateManagerCommand>
{
    public UpdateManagerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Phone).MaximumLength(100);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(320);
    }
}
