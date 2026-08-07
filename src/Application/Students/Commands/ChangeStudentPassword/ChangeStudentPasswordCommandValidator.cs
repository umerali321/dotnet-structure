using FluentValidation;

namespace SkillsetsBackend.Application.Students.Commands.ChangeStudentPassword;

public class ChangeStudentPasswordCommandValidator : AbstractValidator<ChangeStudentPasswordCommand>
{
    public ChangeStudentPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(500);
    }
}
