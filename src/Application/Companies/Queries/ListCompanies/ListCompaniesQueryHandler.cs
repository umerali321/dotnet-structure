using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Companies.Queries.ListCompanies;

/// <summary>
/// SuperAdmin sees every active company (used to populate the Angular company filter). A Manager
/// only sees companies they actively manage - same set already in their JWT/session, exposed here
/// for a consistent lookup shape. Students have no legitimate use for this list.
/// </summary>
public class ListCompaniesQueryHandler(ICompanyQueryService companyQueryService, IUserDirectory userDirectory)
{
    public async Task<IReadOnlyList<CompanyListItemDto>> Handle(
        ListCompaniesQuery query,
        CallerContext caller,
        CancellationToken cancellationToken = default)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can list companies.");
        }

        var restrictToCompanyIds = caller.IsSuperAdmin
            ? null
            : await StudentAuthorization.GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);

        return await companyQueryService.ListAsync(restrictToCompanyIds, query.Search, cancellationToken);
    }
}
