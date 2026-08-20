using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Students.Commands.AssignStudentManager;

/// <summary>SuperAdmin or CompanyAdmin only - sets/clears which Manager an employee is individually
/// assigned to (StudentProfile.ManagerId). See StudentAuthorization.cs for how this narrows a
/// Manager's visibility once set.</summary>
public class AssignStudentManagerCommandHandler
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public AssignStudentManagerCommandHandler(IStudentRepository studentRepository, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _studentRepository = studentRepository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task Handle(int studentUserId, AssignStudentManagerCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Students.AssignManager, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to assign an employee to a Manager.");
        }

        var profile = await _studentRepository.GetProfileByUserIdAsync(studentUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Identity.StudentProfile), studentUserId);

        var studentCompanies = await _userDirectory.GetActiveCompanyRolesAsync(studentUserId, cancellationToken);

        IReadOnlyCollection<int>? managedCompanyIds = null;
        if (!caller.IsSuperAdmin)
        {
            managedCompanyIds = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);
            if (!studentCompanies.Any(c => managedCompanyIds.Contains(c.CompanyId)))
            {
                throw new UnauthorizedAccessException("You do not have access to this employee.");
            }
        }

        if (command.ManagerId is int managerId)
        {
            var managerCompanies = await _userDirectory.GetActiveCompanyRolesAsync(managerId, cancellationToken);
            var sharedManagerCompanyIds = managerCompanies
                .Where(c => Roles.Normalize(c.RoleName) == Roles.Manager)
                .Select(c => c.CompanyId)
                .Where(id => studentCompanies.Any(sc => sc.CompanyId == id))
                .ToList();

            if (sharedManagerCompanyIds.Count == 0)
            {
                throw new ConflictException("The selected user is not an active Manager at this employee's company.");
            }

            if (managedCompanyIds is not null && !sharedManagerCompanyIds.Any(id => managedCompanyIds.Contains(id)))
            {
                throw new UnauthorizedAccessException("You do not have access to that Manager.");
            }
        }

        profile.AssignManager(command.ManagerId, caller.Email);
        await _studentRepository.SaveChangesAsync(cancellationToken);
    }
}
