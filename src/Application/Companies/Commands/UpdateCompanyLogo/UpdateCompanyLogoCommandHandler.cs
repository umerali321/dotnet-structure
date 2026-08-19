using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Companies.Commands.UpdateCompanyLogo;

/// <summary>Persists an already-saved logo's URL against the company. The actual file write (disk
/// path, content-type/size validation) happens in the API layer, which is where IFormFile/hosting
/// concerns naturally live - this handler only records the resulting URL.</summary>
public class UpdateCompanyLogoCommandHandler
{
    private readonly ICompanyRepository _repository;

    public UpdateCompanyLogoCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(int companyId, string logoUrl, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can update a company's logo.");
        }

        var company = await _repository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new NotFoundException("Company", companyId);

        company.UpdateLogo(logoUrl);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
