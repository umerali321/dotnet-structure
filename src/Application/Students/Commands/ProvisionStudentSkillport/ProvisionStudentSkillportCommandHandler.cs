using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Common.Exceptions;
using SkillsetsBackend.Application.Skillsoft;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Students.Commands.ProvisionStudentSkillport;

public class ProvisionStudentSkillportCommandHandler
{
    private readonly IValidator<ProvisionStudentSkillportCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly ISkillsoftProvisioningService _skillsoftProvisioningService;

    public ProvisionStudentSkillportCommandHandler(
        IValidator<ProvisionStudentSkillportCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory,
        ISkillsoftProvisioningService skillsoftProvisioningService)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
        _skillsoftProvisioningService = skillsoftProvisioningService;
    }

    public async Task<SkillsoftProvisionResult> Handle(ProvisionStudentSkillportCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can provision Skillport accounts.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageStudentAsync(caller, command.UserId, _userDirectory, cancellationToken);
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);
        }

        var user = await _repository.GetUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), command.UserId);

        if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Username))
        {
            return new SkillsoftProvisionResult(false, "This student has no email or username on file.");
        }

        var (managerEmail, managerName) = await CallerIdentityResolver.ResolveAsync(caller, _userDirectory, cancellationToken);

        return await _skillsoftProvisioningService.ProvisionAsync(
            new SkillsoftProvisionRequest(
                command.CompanyId, user.Username, command.Password, user.FirstName ?? string.Empty, user.LastName ?? string.Empty,
                user.Email, managerEmail, managerName),
            cancellationToken);
    }
}
