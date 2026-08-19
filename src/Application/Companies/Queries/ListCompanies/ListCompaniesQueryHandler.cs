using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Companies.DTOs;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Companies.Queries.ListCompanies;

/// <summary>
/// SuperAdmin sees every active company (used to populate the Angular company filter). A Manager or
/// CompanyAdmin only sees companies they actively manage - same set already in their JWT/session,
/// exposed here for a consistent lookup shape (e.g. the Company Admins grid's logo lookup on the
/// customer app). Students have no legitimate use for this list.
/// </summary>
public class ListCompaniesQueryHandler(ICompanyQueryService companyQueryService, IUserDirectory userDirectory)
{
    public async Task<PaginatedList<CompanyListItemDto>> Handle(
        ListCompaniesQuery query,
        CallerContext caller,
        CancellationToken cancellationToken = default)
    {
        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin, company managers, and company admins can list companies.");
        }

        var restrictToCompanyIds = caller.IsSuperAdmin
            ? null
            : await StudentAuthorization.GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);

        // Only SuperAdmin's explicit request to include inactive companies (the company-management
        // screen) is honored - every other caller keeps seeing active-only, unchanged. Same for the
        // status filter - it's a company-management-screen concept.
        var includeInactive = caller.IsSuperAdmin && query.IncludeInactive;
        var statusFilter = caller.IsSuperAdmin ? query.StatusFilter : null;
        var page = Math.Max(1, query.Page);
        var pageSize = query.PageSize <= 0 ? 100 : Math.Min(5000, query.PageSize);

        return await companyQueryService.ListAsync(restrictToCompanyIds, query.Search, includeInactive, statusFilter, page, pageSize, cancellationToken);
    }
}
