using FluentValidation;

namespace SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;

public class ChangeManagerPasswordCommandValidator : AbstractValidator<ChangeManagerPasswordCommand>
{
    public ChangeManagerPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(500);
    }
}
