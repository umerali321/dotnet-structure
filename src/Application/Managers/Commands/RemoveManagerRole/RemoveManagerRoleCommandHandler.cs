using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Commands.RemoveManagerRole;

public class RemoveManagerRoleCommandHandler
{
    private readonly IManagerRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public RemoveManagerRoleCommandHandler(IManagerRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(RemoveManagerRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and CompanyAdmin can revoke a Manager role from a user.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);
        }

        var user = await _repository.GetUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), command.UserId);

        var activeRoles = await _userDirectory.GetActiveCompanyRolesAsync(command.UserId, cancellationToken);
        var isManagerHere = activeRoles.Any(r => r.CompanyId == command.CompanyId && r.RoleName == Roles.Manager);
        if (!isManagerHere)
        {
            return;
        }

        if (activeRoles.Count == 1)
        {
            throw new ConflictException("This is this person's only active role. Grant another role before removing this one.");
        }

        await _repository.RemoveManagerRoleAsync(command.UserId, command.CompanyId, cancellationToken);
    }
}
