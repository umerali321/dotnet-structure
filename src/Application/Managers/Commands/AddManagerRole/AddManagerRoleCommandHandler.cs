using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Managers.Commands.AddManagerRole;

public class AddManagerRoleCommandHandler
{
    private readonly IManagerRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public AddManagerRoleCommandHandler(IManagerRepository repository, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _repository = repository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task Handle(AddManagerRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
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

        _ = await _repository.GetUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), command.UserId);

        // A person can hold Employee, Manager, and Company Admin at the same company at once - no
        // exclusivity between them, so granting one never blocks granting another.
        await _repository.AddManagerRoleAsync(command.UserId, command.CompanyId, startDate: null, cancellationToken);
    }
}
