using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Commands.RemoveCompanyAdminRole;

public class RemoveCompanyAdminRoleCommandHandler
{
    private readonly IManagerRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public RemoveCompanyAdminRoleCommandHandler(IManagerRepository repository, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _repository = repository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task Handle(RemoveCompanyAdminRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        // Roles.Assign, not a hardcoded SuperAdmin/CompanyAdmin pair: whoever is granted
        // "Assign Roles to Users" can do this, and whoever is not cannot - which is what
        // makes the assign icon on the Manager/Employee screens meaningful.
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Roles.Assign, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to assign roles to users.");
        }

        if (!caller.IsPlatformAdmin)
        {
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);
        }

        var user = await _repository.GetUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), command.UserId);

        var activeRoles = await _userDirectory.GetActiveCompanyRolesAsync(command.UserId, cancellationToken);
        var isCompanyAdminHere = activeRoles.Any(r => r.CompanyId == command.CompanyId && r.RoleName == Roles.CompanyAdmin);
        if (!isCompanyAdminHere)
        {
            return;
        }

        if (activeRoles.Count == 1)
        {
            throw new ConflictException("This is this person's only active role. Grant another role before removing this one.");
        }

        await _repository.RemoveCompanyAdminRoleAsync(command.UserId, command.CompanyId, cancellationToken);
    }
}
