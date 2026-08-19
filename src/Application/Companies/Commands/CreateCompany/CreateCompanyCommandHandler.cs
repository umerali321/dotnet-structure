using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandHandler
{
    private readonly IValidator<CreateCompanyCommand> _validator;
    private readonly ICompanyRepository _repository;

    public CreateCompanyCommandHandler(IValidator<CreateCompanyCommand> validator, ICompanyRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task<int> Handle(CreateCompanyCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can create a company.");
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (await _repository.CompanyCodeExistsAsync(command.CompanyCode, excludeCompanyId: null, cancellationToken))
        {
            throw new AppValidationException(
            [
                new ValidationFailure(nameof(command.CompanyCode), "This company code is already in use."),
            ]);
        }

        if (await _repository.IdentifierInUseAsync(command.AdminEmail, command.AdminUsername, cancellationToken))
        {
            throw new AppValidationException(
            [
                new ValidationFailure(nameof(command.AdminUsername), "Email or username is already in use."),
            ]);
        }

        var company = Company.Create(
            command.CompanyCode, command.CompanyName, command.CompanyEmail, command.CompanyPhone,
            command.PlanType, command.LicenseStartDate, command.LicenseEndDate);
        var admin = AppUser.CreateStudent(
            command.AdminEmail, phone: null, command.AdminFirstName, command.AdminLastName, command.AdminUsername, command.AdminPassword);

        return await _repository.CreateCompanyWithAdminAsync(company, admin, cancellationToken);
    }
}
