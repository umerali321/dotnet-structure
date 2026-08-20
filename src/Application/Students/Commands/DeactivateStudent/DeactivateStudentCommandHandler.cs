using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Students.Commands.DeactivateStudent;

public class DeactivateStudentCommandHandler
{
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public DeactivateStudentCommandHandler(IStudentRepository repository, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _repository = repository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Students.Delete, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to deactivate employees.");
        }

        await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, _repository, cancellationToken);

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        user.Deactivate();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
