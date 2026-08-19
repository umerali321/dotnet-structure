using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.RoleManagement.Commands.DeactivateRole;

/// <summary>SuperAdmin only. Soft delete - the role and its RolePermissions stay intact so it can
/// be reactivated later. System roles can never be deactivated.</summary>
public class DeactivateRoleCommandHandler
{
    private readonly IRoleRepository _repository;

    public DeactivateRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(byte roleId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can delete roles.");
        }

        var role = await _repository.GetTrackedRoleByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException("Role", roleId);

        if (role.IsSystemRole)
        {
            throw new ConflictException("System roles cannot be deleted.");
        }

        role.Deactivate();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
