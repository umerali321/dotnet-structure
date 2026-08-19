using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Application.Companies.Interfaces;

namespace SkillsetsBackend.Application.Companies.Queries.GetCompanyById;

public class GetCompanyByIdQueryHandler
{
    private readonly ICompanyRepository _repository;

    public GetCompanyByIdQueryHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task<CompanyListItemDto?> Handle(int companyId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can view a company's details.");
        }

        var company = await _repository.GetByIdAsync(companyId, cancellationToken);
        return company is null
            ? null
            : new CompanyListItemDto(
                company.CompanyId, company.CompanyCode, company.CompanyName, company.IsActive, company.LogoUrl,
                company.PlanType, company.PlanStartDate, company.PlanEndDate, company.IsExpired,
                company.CompanyEmail, company.CompanyPhone, company.Street1, company.Street2,
                company.City, company.State, company.Zip, company.PaymentForm, company.TotalPayment);
    }
}
