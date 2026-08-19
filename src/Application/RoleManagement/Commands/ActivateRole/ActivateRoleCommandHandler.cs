using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.RoleManagement.Commands.ActivateRole;

public class ActivateRoleCommandHandler
{
    private readonly IRoleRepository _repository;

    public ActivateRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(byte roleId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can restore roles.");
        }

        var role = await _repository.GetTrackedRoleByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException("Role", roleId);

        role.Activate();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
