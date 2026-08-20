using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Managers.Queries.GetManagerCredentials;

public class GetManagerCredentialsQueryHandler
{
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public GetManagerCredentialsQueryHandler(IStudentRepository repository, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _repository = repository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<ManagerCredentialDto> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Managers.ViewCredentials, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view manager login credentials.");
        }

        await StudentAuthorization.EnsureCanManageManagerAsync(caller, userId, _userDirectory, cancellationToken);

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        return new ManagerCredentialDto(user.Username, user.Email, user.PasswordHash, user.IsActive);
    }
}
