using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.RoleManagement.Commands.UpdateRolePermissions;

/// <summary>SuperAdmin only. System roles (Student/Manager/FDM/Admin/CompanyAdmin) are now editable
/// too, same as custom roles - HasPermissionAsync is the live source of truth wherever a handler has
/// been converted to call it (see CreateManagerCommandHandler, ListManagersQueryHandler,
/// CreateStudentCommandHandler, ListStudentsQueryHandler for the first batch), so toggling a
/// checkbox here takes effect immediately for those actions. Actions not yet converted still run on
/// their existing hardcoded role checks regardless of what's toggled here - not every permission in
/// the dialog is wired up yet, that's an ongoing migration, not a one-time cutover.</summary>
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

        await _repository.ReplaceRolePermissionsAsync(roleId, command.PermissionIds, cancellationToken);
    }
}
