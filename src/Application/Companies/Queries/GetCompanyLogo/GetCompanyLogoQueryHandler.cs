using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.Interfaces;

namespace SkillsetsBackend.Application.Companies.Queries.GetCompanyLogo;

/// <summary>Deliberately open to any authenticated caller (not company-scoped like
/// GetCompanyByIdQueryHandler) - a logo URL alone carries no sensitive data (the image itself is
/// already served as an unauthenticated static file, see CompaniesController.UploadLogo), so this
/// exists purely so the header can show a company's logo without needing SuperAdmin-only access to
/// the full company record (which does carry billing/address fields).</summary>
public class GetCompanyLogoQueryHandler
{
    private readonly ICompanyRepository _repository;

    public GetCompanyLogoQueryHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task<string?> Handle(int companyId, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(companyId, cancellationToken);
        return company?.LogoUrl;
    }
}
