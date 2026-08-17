using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using AuthenticationFailedException = SkillsetsBackend.Application.Common.Exceptions.AuthenticationFailedException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;

public class ChangeManagerPasswordCommandHandler
{
    private readonly IValidator<ChangeManagerPasswordCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly ILegacyCredentialVerifier _credentialVerifier;

    public ChangeManagerPasswordCommandHandler(
        IValidator<ChangeManagerPasswordCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory,
        ILegacyCredentialVerifier credentialVerifier)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
        _credentialVerifier = credentialVerifier;
    }

    public async Task Handle(int userId, ChangeManagerPasswordCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can change manager passwords.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageManagerAsync(caller, userId, _userDirectory, cancellationToken);
        }

        if (caller.DbUserId == userId)
        {
            // Self-service change - current password is required, not merely verified-if-present,
            // otherwise omitting it from the request would silently skip verification entirely.
            if (string.IsNullOrEmpty(command.CurrentPassword)
                || string.IsNullOrEmpty(user.PasswordHash)
                || !_credentialVerifier.Verify(command.CurrentPassword, user.PasswordHash))
            {
                throw new AuthenticationFailedException("Current password is incorrect.");
            }
        }

        user.SetPassword(command.NewPassword);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
