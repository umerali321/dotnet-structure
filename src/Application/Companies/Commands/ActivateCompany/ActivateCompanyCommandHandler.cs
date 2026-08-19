using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Companies.Commands.ActivateCompany;

public class ActivateCompanyCommandHandler
{
    private readonly ICompanyRepository _repository;

    public ActivateCompanyCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(int companyId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can reactivate a company.");
        }

        var company = await _repository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new NotFoundException("Company", companyId);

        company.Activate();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
