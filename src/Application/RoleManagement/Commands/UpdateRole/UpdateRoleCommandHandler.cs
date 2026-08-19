using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.RoleManagement.Commands.UpdateRole;

/// <summary>SuperAdmin only - renames a custom role. System roles are rejected, matching
/// UpdateRolePermissionsCommandHandler's protection (their name is part of the hardcoded
/// authorization checks elsewhere - see AGENTS.md).</summary>
public class UpdateRoleCommandHandler
{
    private readonly IValidator<UpdateRoleCommand> _validator;
    private readonly IRoleRepository _repository;

    public UpdateRoleCommandHandler(IValidator<UpdateRoleCommand> validator, IRoleRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task Handle(byte roleId, UpdateRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can rename roles.");
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var role = await _repository.GetTrackedRoleByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException("Role", roleId);

        if (role.IsSystemRole)
        {
            throw new ConflictException("System roles cannot be renamed.");
        }

        if (await _repository.RoleNameExistsAsync(command.RoleName, roleId, cancellationToken))
        {
            throw new AppValidationException([new ValidationFailure(nameof(command.RoleName), "A role with this name already exists.")]);
        }

        role.Rename(command.RoleName);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
