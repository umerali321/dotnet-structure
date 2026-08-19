using FluentValidation;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Companies.Commands.SetCompanyLicense;

public class SetCompanyLicenseCommandHandler
{
    private readonly IValidator<SetCompanyLicenseCommand> _validator;
    private readonly ICompanyRepository _repository;

    public SetCompanyLicenseCommandHandler(IValidator<SetCompanyLicenseCommand> validator, ICompanyRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task Handle(int companyId, SetCompanyLicenseCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can set a company's license.");
        }

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var company = await _repository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new NotFoundException("Company", companyId);

        company.SetLicense(command.StartDate, command.EndDate);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
