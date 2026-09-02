using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.SystemAdmins.Commands.ResetSystemAdminPassword;

/// <summary>
/// SuperAdmin-only, like everything else about System Administrators (see SystemAdminAuthorization).
///
/// Also refuses to touch anyone who is not actually a System Administrator, so this endpoint can
/// never be pointed at an ordinary employee or manager to change their password - those have their
/// own flows with their own permission checks.
/// </summary>
public class ResetSystemAdminPasswordCommandHandler
{
    private readonly IValidator<ResetSystemAdminPasswordCommand> _validator;
    private readonly IManagerRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public ResetSystemAdminPasswordCommandHandler(
        IValidator<ResetSystemAdminPasswordCommand> validator,
        IManagerRepository repository,
        IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(
        int userId, ResetSystemAdminPasswordCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        SystemAdminAuthorization.EnsureSuperAdmin(caller);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var roles = await _userDirectory.GetActiveCompanyRolesAsync(userId, cancellationToken);
        if (!roles.Any(r => r.RoleName == Roles.SystemAdmin))
        {
            throw new NotFoundException("System Administrator", userId);
        }

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        user.SetPassword(command.NewPassword);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
