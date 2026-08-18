using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.RoleManagement.Commands.UpdateRolePermissions;

/// <summary>SuperAdmin only. The 5 seeded system roles (Student/Manager/FDM/Admin/CompanyAdmin) are
/// rejected - their RolePermissions were seeded to mirror the existing hardcoded authorization
/// checks exactly (see RolePermissionConfiguration.cs); editing them here would silently desync
/// HasPermissionAsync from what those checks actually do, which is exactly the drift the phase-1
/// plan avoids. Only custom roles created via CreateRole are editable in this phase.</summary>
public class UpdateRolePermissionsCommandHandler
{
    private readonly IRoleRepository _repository;

    public UpdateRolePermissionsCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(byte roleId, UpdateRolePermissionsCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can edit role permissions.");
        }

        var role = await _repository.GetRoleByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException("Role", roleId);

        if (role.IsSystemRole)
        {
            throw new ConflictException("System roles cannot be edited in this phase.");
        }

        await _repository.ReplaceRolePermissionsAsync(roleId, command.PermissionIds, cancellationToken);
    }
}
