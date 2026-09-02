using FluentValidation;

namespace SkillsetsBackend.Application.SystemAdmins.Commands.CreateSystemAdmin;

public class CreateSystemAdminCommandValidator : AbstractValidator<CreateSystemAdminCommand>
{
    public CreateSystemAdminCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(500);
    }
}
