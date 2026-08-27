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

    public AddManagerRoleCommandHandler(IManagerRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(AddManagerRoleCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and CompanyAdmin can grant a Manager role to an existing user.");
        }

        if (!caller.IsSuperAdmin)
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
