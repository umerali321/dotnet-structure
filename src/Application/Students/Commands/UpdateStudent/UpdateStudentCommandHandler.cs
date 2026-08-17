using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Students.Commands.UpdateStudent;

public class UpdateStudentCommandHandler
{
    private readonly IValidator<UpdateStudentCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public UpdateStudentCommandHandler(
        IValidator<UpdateStudentCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(int userId, UpdateStudentCommand command, CallerContext caller, CancellationToken cancellationToken)
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
                throw new UnauthorizedAccessException("You can only update your own profile.");
            }

            var currentProfile = await _repository.GetProfileByUserIdAsync(userId, cancellationToken);
            var restrictedFieldsChanged =
                command.Email != user.Email ||
                command.Username != user.Username ||
                command.StudentType != currentProfile?.StudentType;

            if (restrictedFieldsChanged)
            {
                throw new UnauthorizedAccessException("You are only allowed to update your first name, last name, and phone.");
            }

            user.UpdatePersonalInfo(command.FirstName, command.LastName, command.Phone);
        }
        else
        {
            await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, cancellationToken);

            // Email/Username are the account identifier - fixed at creation, never editable
            // afterward (by anyone, including admins) so history/assignments/integrations never
            // risk forking onto a second identity. Silently keep the existing values regardless
            // of what the client sends, rather than trusting the request payload.
            user.UpdateProfile(user.Email!, command.Phone, command.FirstName, command.LastName, user.Username!);

            var profile = await _repository.GetProfileByUserIdAsync(userId, cancellationToken);
            profile?.Update(command.StudentType, caller.Email);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
