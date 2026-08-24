using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SkillTrax.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using ConflictException = SkillsetsBackend.Application.Common.Exceptions.ConflictException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.SkillTrax.Commands.DeleteSkillTrax;

/// <summary>Blocks deletion while a non-cancelled assignment still references this SkillTrax (see
/// ISkillTraxRepository.HasActiveAssignmentAsync) - historical/cancelled assignments never block a
/// delete since AssignmentTitles already holds an independent CourseId snapshot.</summary>
public class DeleteSkillTraxCommandHandler
{
    private readonly ISkillTraxRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public DeleteSkillTraxCommandHandler(ISkillTraxRepository repository, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _repository = repository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task Handle(int skillTraxId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.SkillTrax.Delete, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to delete a SkillTrax.");
        }

        var skillTrax = await _repository.GetByIdAsync(skillTraxId, cancellationToken)
            ?? throw new NotFoundException("SkillTrax", skillTraxId);

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, skillTrax.CompanyId, _userDirectory, cancellationToken);

        if (await _repository.HasActiveAssignmentAsync(skillTraxId, cancellationToken))
        {
            throw new ConflictException("This SkillTrax has an active or scheduled assignment - it cannot be deleted until that assignment ends or is cancelled.");
        }

        await _repository.DeleteAsync(skillTrax, cancellationToken);
    }
}
