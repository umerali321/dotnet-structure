using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Managers.Commands.CreateManager;

/// <summary>SkillportProvisioned means the Skillport account was created - the 30-day session itself stays dormant until they first enter the course library.</summary>
public record CreateManagerResult(int UserId, bool SkillportRequested, bool SkillportProvisioned, string? SkillportError);

public class CreateManagerCommandHandler
{
    private readonly IValidator<CreateManagerCommand> _validator;
    private readonly IManagerRepository _repository;
    private readonly IStudentRepository _studentRepository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;
    private readonly ISkillportSessionService _skillportSessionService;

    public CreateManagerCommandHandler(
        IValidator<CreateManagerCommand> validator,
        IManagerRepository repository,
        IStudentRepository studentRepository,
        IUserDirectory userDirectory,
        IPermissionService permissionService,
        ISkillportSessionService skillportSessionService)
    {
        _validator = validator;
        _repository = repository;
        _studentRepository = studentRepository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
        _skillportSessionService = skillportSessionService;
    }

    public async Task<CreateManagerResult> Handle(CreateManagerCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        // Permission-driven (RolePermissions), not a hardcoded role check - a SuperAdmin can grant or
        // revoke "Create Managers" for the Manager role from the Roles & Permissions screen and this
        // takes effect immediately, no code change needed. SuperAdmin itself always bypasses (see
        // PermissionService).
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Managers.Create, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to create managers.");
        }

        // A CompanyAdmin can create another CompanyAdmin; a Manager (even with "Create Managers")
        // cannot - prevents a lower-privileged role from creating a peer of a higher one.
        if (command.Role == Roles.CompanyAdmin && !caller.IsSuperAdmin && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company admins can create company admins.");
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

        var userId = await _repository.CreateManagerAsync(
            user,
            command.CompanyId,
            command.StartDate,
            command.Role,
            cancellationToken);

        if (command.AlsoCreateEmployee)
        {
            await _studentRepository.AddEmployeeRoleAsync(userId, command.CompanyId, caller.Email, startDate: null, cancellationToken);
        }

        if (!command.CreateInSkillport)
        {
            return new CreateManagerResult(userId, SkillportRequested: false, SkillportProvisioned: false, SkillportError: null);
        }

        // Best-effort: the manager account is already created above regardless of what happens here.
        // This only creates the Skillport account (dormant) - the session activates on their first visit.
        try
        {
            var provisionResult = await _skillportSessionService.EnsureDormantAccountAsync(userId, command.CompanyId, cancellationToken);

            return new CreateManagerResult(userId, SkillportRequested: true, provisionResult.Success, provisionResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new CreateManagerResult(userId, SkillportRequested: true, SkillportProvisioned: false, ex.Message);
        }
    }
}
