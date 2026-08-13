using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Managers.Commands.UpdateManager;

public class UpdateManagerCommandHandler
{
    private readonly IValidator<UpdateManagerCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public UpdateManagerCommandHandler(
        IValidator<UpdateManagerCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task Handle(int userId, UpdateManagerCommand command, CallerContext caller, CancellationToken cancellationToken)
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
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can update managers.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageManagerAsync(caller, userId, _userDirectory, cancellationToken);
        }

        if (await _repository.IdentifierInUseAsync(command.Email, command.Username, userId, cancellationToken))
        {
            throw new AppValidationException(
            [
                new ValidationFailure(nameof(command.Username), "Email or username is already in use."),
            ]);
        }

        user.UpdateProfile(command.Email, null, command.FirstName, command.LastName, command.Username);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
