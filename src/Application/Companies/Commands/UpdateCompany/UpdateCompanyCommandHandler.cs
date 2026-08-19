using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandHandler
{
    private readonly IValidator<UpdateCompanyCommand> _validator;
    private readonly ICompanyRepository _repository;

    public UpdateCompanyCommandHandler(IValidator<UpdateCompanyCommand> validator, ICompanyRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task Handle(int companyId, UpdateCompanyCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can edit a company.");
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var company = await _repository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new NotFoundException("Company", companyId);

        if (await _repository.CompanyCodeExistsAsync(command.CompanyCode, excludeCompanyId: companyId, cancellationToken))
        {
            throw new AppValidationException(
            [
                new ValidationFailure(nameof(command.CompanyCode), "This company code is already in use."),
            ]);
        }

        company.UpdateDetails(
            command.CompanyCode, command.CompanyName, command.CompanyEmail, command.CompanyPhone,
            command.Street1, command.Street2, command.City, command.State, command.Zip,
            command.PaymentForm, command.TotalPayment);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
