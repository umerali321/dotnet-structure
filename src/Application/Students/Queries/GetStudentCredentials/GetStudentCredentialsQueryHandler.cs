using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Students.Queries.GetStudentCredentials;

public class GetStudentCredentialsQueryHandler
{
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public GetStudentCredentialsQueryHandler(IStudentRepository repository, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _repository = repository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<StudentCredentialDto> Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Students.ViewCredentials, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view employee login credentials.");
        }

        await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, _repository, cancellationToken);

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        return new StudentCredentialDto(user.Username, user.Email, user.PasswordHash, user.IsActive);
    }
}
