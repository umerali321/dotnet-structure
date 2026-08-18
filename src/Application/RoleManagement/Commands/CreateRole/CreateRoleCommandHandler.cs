using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.RoleManagement.Commands.CreateRole;

/// <summary>SuperAdmin only - see Domain/Identity/Role.cs (Create factory) and AGENTS.md: Roles is a
/// read-only entity by default, this is the deliberate, narrow write path added for this feature.</summary>
public class CreateRoleCommandHandler
{
    private readonly IValidator<CreateRoleCommand> _validator;
    private readonly IRoleRepository _repository;

    public CreateRoleCommandHandler(IValidator<CreateRoleCommand> validator, IRoleRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task<byte> Handle(CreateRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can create roles.");
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (await _repository.RoleNameExistsAsync(command.RoleName, cancellationToken))
        {
            throw new AppValidationException([new ValidationFailure(nameof(command.RoleName), "A role with this name already exists.")]);
        }

        var role = Role.Create(command.RoleName);
        var roleId = await _repository.AddRoleAsync(role, cancellationToken);

        if (command.PermissionIds.Count > 0)
        {
            await _repository.ReplaceRolePermissionsAsync(roleId, command.PermissionIds, cancellationToken);
        }

        return roleId;
    }
}
