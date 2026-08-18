using FluentValidation;

namespace SkillsetsBackend.Application.RoleManagement.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.RoleName).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.PermissionIds).GreaterThan(0);
    }
}
