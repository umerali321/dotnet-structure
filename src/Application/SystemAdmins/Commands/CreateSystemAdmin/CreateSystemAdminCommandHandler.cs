using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.SystemAdmins.Commands.CreateSystemAdmin;

public record CreateSystemAdminResult(int UserId);

/// <summary>
/// Creates a SystemAdmin: SuperAdmin's delegate in the admin application. The account is created
/// with NO permissions of its own - everything it can do comes from what the SuperAdmin then grants
/// the SystemAdmin role in Roles &amp; Permissions.
/// </summary>
public class CreateSystemAdminCommandHandler
{
    private readonly IValidator<CreateSystemAdminCommand> _validator;
    private readonly IManagerRepository _repository;
    private readonly ICompanyQueryService _companyQueryService;
    private readonly AccountWelcomeEmail _welcomeEmail;

    public CreateSystemAdminCommandHandler(
        IValidator<CreateSystemAdminCommand> validator,
        IManagerRepository repository,
        ICompanyQueryService companyQueryService,
        AccountWelcomeEmail welcomeEmail)
    {
        _validator = validator;
        _repository = repository;
        _companyQueryService = companyQueryService;
        _welcomeEmail = welcomeEmail;
    }

    public async Task<CreateSystemAdminResult> Handle(
        CreateSystemAdminCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        // Hardcoded, not permission-driven - see SystemAdminAuthorization for why.
        SystemAdminAuthorization.EnsureSuperAdmin(caller);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        // The email doubles as the username - see the command's remarks.
        var username = command.Email.Trim();

        if (await _repository.IdentifierInUseAsync(command.Email, username, excludeUserId: null, cancellationToken))
        {
            throw new AppValidationException(
            [
                new ValidationFailure(nameof(command.Email), "That email is already in use."),
            ]);
        }

        var user = AppUser.CreateStudent(
            command.Email, command.Phone, command.FirstName, command.LastName, username, command.Password);

        // A System Administrator belongs to no company, but every DB-backed account resolves its role
        // through a UserCompanyRoles row - so one is attached here to whichever company happens to be
        // first, purely so login works. Nobody is asked to choose it and nothing scopes by it: a
        // SystemAdmin sees every company (CallerContext.HasGlobalCompanyScope).
        var companies = await _companyQueryService.ListAsync(
            restrictToCompanyIds: null, search: null, includeInactive: false, statusFilter: null,
            page: 1, pageSize: 1, cancellationToken);

        var carrierCompany = companies.Items.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Cannot create a System Administrator until at least one active company exists - the account " +
                "needs a company membership row to resolve its role at login.");

        // Reuses the Managers repository: same Users + UserCompanyRoles write, different role name.
        var userId = await _repository.CreateManagerAsync(
            user, carrierCompany.CompanyId, startDate: null, Roles.SystemAdmin, cancellationToken);

        await _welcomeEmail.SendAsync(command.Email, command.FirstName, command.Password, cancellationToken);

        return new CreateSystemAdminResult(userId);
    }
}
