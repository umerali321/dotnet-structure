using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Students.Commands.CreateStudent;

public record CreateStudentResult(int UserId);

public class CreateStudentCommandHandler
{
    private readonly IValidator<CreateStudentCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IManagerRepository _managerRepository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;
    private readonly AccountWelcomeEmail _welcomeEmail;

    public CreateStudentCommandHandler(
        IValidator<CreateStudentCommand> validator,
        IStudentRepository repository,
        IManagerRepository managerRepository,
        IUserDirectory userDirectory,
        IPermissionService permissionService,
        AccountWelcomeEmail welcomeEmail)
    {
        _validator = validator;
        _repository = repository;
        _managerRepository = managerRepository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
        _welcomeEmail = welcomeEmail;
    }

    public async Task<CreateStudentResult> Handle(CreateStudentCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        // Permission-driven (RolePermissions), not a hardcoded role check - see
        // CreateManagerCommandHandler for the identical pattern.
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Students.Create, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to create employees.");
        }

        if (command.AlsoCreateManager && !caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Managers.Create, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to create managers.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);
        }

        if (await _repository.IdentifierInUseAsync(command.Email, command.Username, excludeUserId: null, cancellationToken))
        {
            throw new AppValidationException(
            [
                new ValidationFailure(nameof(command.Username), "Email or username is already in use."),
            ]);
        }

        var user = AppUser.CreateStudent(command.Email, command.Phone, command.FirstName, command.LastName, command.Username, command.Password);

        var userId = await _repository.CreateStudentAsync(
            user, command.StudentType, caller.Email, command.CompanyId, command.StartDate, cancellationToken);

        if (command.AlsoCreateManager)
        {
            await _managerRepository.AddManagerRoleAsync(userId, command.CompanyId, startDate: null, cancellationToken);
        }

        // Only once the account genuinely exists - and with the password that was actually stored,
        // whether the admin typed it or let the form generate one.
        await _welcomeEmail.SendAsync(command.Email, command.FirstName, command.Password, cancellationToken);

        return new CreateStudentResult(userId);
    }
}
