using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Application.Skillsoft;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Managers.Commands.CreateManager;

public record CreateManagerResult(int UserId, bool SkillportRequested, bool SkillportProvisioned, string? SkillportError);

public class CreateManagerCommandHandler
{
    private readonly IValidator<CreateManagerCommand> _validator;
    private readonly IManagerRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly ISkillsoftProvisioningService _skillsoftProvisioningService;

    public CreateManagerCommandHandler(
        IValidator<CreateManagerCommand> validator,
        IManagerRepository repository,
        IUserDirectory userDirectory,
        ISkillsoftProvisioningService skillsoftProvisioningService)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
        _skillsoftProvisioningService = skillsoftProvisioningService;
    }

    public async Task<CreateManagerResult> Handle(CreateManagerCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can create managers.");
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
            cancellationToken);

        if (!command.CreateInSkillport)
        {
            return new CreateManagerResult(userId, SkillportRequested: false, SkillportProvisioned: false, SkillportError: null);
        }

        // Best-effort: the manager account is already created above regardless of what happens here.
        try
        {
            var (managerEmail, managerName) = await CallerIdentityResolver.ResolveAsync(caller, _userDirectory, cancellationToken);
            var provisionResult = await _skillsoftProvisioningService.ProvisionAsync(
                new SkillsoftProvisionRequest(
                    command.CompanyId, command.Username, command.Password, command.FirstName, command.LastName, command.Email,
                    managerEmail, managerName),
                cancellationToken);

            return new CreateManagerResult(userId, SkillportRequested: true, provisionResult.Success, provisionResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new CreateManagerResult(userId, SkillportRequested: true, SkillportProvisioned: false, ex.Message);
        }
    }
}
