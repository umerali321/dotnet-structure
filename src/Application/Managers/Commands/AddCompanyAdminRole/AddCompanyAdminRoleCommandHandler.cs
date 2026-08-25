using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Managers.Commands.AddCompanyAdminRole;

public class AddCompanyAdminRoleCommandHandler
{
    private readonly IManagerRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public AddCompanyAdminRoleCommandHandler(IManagerRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(AddCompanyAdminRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and CompanyAdmin can grant a Company Admin role to an existing user.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);
        }

        var user = await _repository.GetUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), command.UserId);

        var activeRoles = await _userDirectory.GetActiveCompanyRolesAsync(command.UserId, cancellationToken);
        if (activeRoles.Any(r => r.CompanyId == command.CompanyId && (r.RoleName == Roles.Manager || r.RoleName == Roles.CompanyAdmin)))
        {
            throw new ConflictException("This person is already a Manager or Company Admin at this company.");
        }

        await _repository.AddCompanyAdminRoleAsync(command.UserId, command.CompanyId, startDate: null, cancellationToken);
    }
}
