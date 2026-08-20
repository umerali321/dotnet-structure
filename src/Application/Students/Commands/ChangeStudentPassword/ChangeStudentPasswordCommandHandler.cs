using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using AuthenticationFailedException = SkillsetsBackend.Application.Common.Exceptions.AuthenticationFailedException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Students.Commands.ChangeStudentPassword;

public class ChangeStudentPasswordCommandHandler
{
    private readonly IValidator<ChangeStudentPasswordCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly ILegacyCredentialVerifier _credentialVerifier;
    private readonly IPermissionService _permissionService;

    public ChangeStudentPasswordCommandHandler(
        IValidator<ChangeStudentPasswordCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory,
        ILegacyCredentialVerifier credentialVerifier,
        IPermissionService permissionService)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
        _credentialVerifier = credentialVerifier;
        _permissionService = permissionService;
    }

    public async Task Handle(int userId, ChangeStudentPasswordCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        if (caller.Role == Roles.Student)
        {
            if (caller.DbUserId != userId)
            {
                throw new UnauthorizedAccessException("You can only change your own password.");
            }

            if (string.IsNullOrEmpty(command.CurrentPassword)
                || string.IsNullOrEmpty(user.PasswordHash)
                || !_credentialVerifier.Verify(command.CurrentPassword, user.PasswordHash))
            {
                throw new AuthenticationFailedException("Current password is incorrect.");
            }
        }
        else
        {
            // A dual-role caller (e.g. Manager also holding Student at the same company) changing
            // their own password never needs the permission - only changing someone else's does.
            if (caller.DbUserId != userId
                && !await _permissionService.HasPermissionAsync(caller, Permissions.Students.ManagePassword, cancellationToken))
            {
                throw new UnauthorizedAccessException("You do not have permission to change employee passwords.");
            }

            // Admin/Manager resets do not require the student's current password.
            await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, _repository, cancellationToken);
        }

        user.SetPassword(command.NewPassword);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
