using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Companies.Commands.DeactivateCompany;

/// <summary>Deactivating a company also blocks login for its members - see
/// LoginCommandHandler/RefreshTokenCommandHandler's HasAnyCompanyRoleAsync check.</summary>
public class DeactivateCompanyCommandHandler
{
    private readonly ICompanyRepository _repository;

    public DeactivateCompanyCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(int companyId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can deactivate a company.");
        }

        var company = await _repository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new NotFoundException("Company", companyId);

        company.Deactivate();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
