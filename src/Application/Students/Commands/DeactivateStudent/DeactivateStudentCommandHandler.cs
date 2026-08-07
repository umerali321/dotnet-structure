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

    public DeactivateStudentCommandHandler(IStudentRepository repository, IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(int userId, CallerContext caller, CancellationToken cancellationToken)
    {
        await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, cancellationToken);

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        user.Deactivate();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
