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

    public ChangeStudentPasswordCommandHandler(
        IValidator<ChangeStudentPasswordCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory,
        ILegacyCredentialVerifier credentialVerifier)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
        _credentialVerifier = credentialVerifier;
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
            // Admin/Manager resets do not require the student's current password.
            await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, cancellationToken);
        }

        user.SetPassword(command.NewPassword);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
