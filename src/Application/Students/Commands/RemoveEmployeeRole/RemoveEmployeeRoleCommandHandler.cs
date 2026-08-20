using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Students.Commands.RemoveEmployeeRole;

public class RemoveEmployeeRoleCommandHandler
{
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public RemoveEmployeeRoleCommandHandler(IStudentRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(RemoveEmployeeRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and CompanyAdmin can revoke an Employee role from a user.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);
        }

        var user = await _repository.GetUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), command.UserId);

        var activeRoles = await _userDirectory.GetActiveCompanyRolesAsync(command.UserId, cancellationToken);
        var isStudentHere = activeRoles.Any(r => r.CompanyId == command.CompanyId && r.RoleName == Roles.Student);
        if (!isStudentHere)
        {
            return;
        }

        if (activeRoles.Count == 1)
        {
            throw new ConflictException("This is this person's only active role. Grant another role before removing this one.");
        }

        await _repository.RemoveEmployeeRoleAsync(command.UserId, command.CompanyId, cancellationToken);
    }
}
