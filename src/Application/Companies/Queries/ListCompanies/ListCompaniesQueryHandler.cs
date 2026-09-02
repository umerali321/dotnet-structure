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
public class ListCompaniesQueryHandler(
    ICompanyQueryService companyQueryService,
    IUserDirectory userDirectory,
    IPermissionService permissionService)
{
    public async Task<PaginatedList<CompanyListItemDto>> Handle(
        ListCompaniesQuery query,
        CallerContext caller,
        CancellationToken cancellationToken = default)
    {
        // A SystemAdmin must actually hold Companies.View - it is not enough to be a SystemAdmin.
        // Manager/CompanyAdmin keep their existing role-based access deliberately: nothing currently
        // grants them Companies.View, and they need this list for their own company pickers, so
        // requiring the permission of everyone would break screens that work today.
        if (caller.IsSystemAdmin)
        {
            if (!await permissionService.HasPermissionAsync(caller, Permissions.Companies.View, cancellationToken))
            {
                throw new UnauthorizedAccessException("You do not have permission to view companies.");
            }
        }
        else if (!caller.IsPlatformAdmin && caller.Role != Roles.Manager && caller.Role != Roles.CompanyAdmin)
        {
            throw new UnauthorizedAccessException("You are not authorized to list companies.");
        }

        // A SystemAdmin administers every company too, so it is not narrowed to its carrier company.
        var restrictToCompanyIds = caller.HasGlobalCompanyScope
            ? null
            : await StudentAuthorization.GetManagedCompanyIdsAsync(caller, userDirectory, cancellationToken);

        // Only the company-management screen (a globally scoped admin) may ask for inactive
        // companies or filter by status - every other caller keeps seeing active-only, unchanged.
        var includeInactive = caller.HasGlobalCompanyScope && query.IncludeInactive;
        var statusFilter = caller.HasGlobalCompanyScope ? query.StatusFilter : null;
        var page = Math.Max(1, query.Page);
        var pageSize = query.PageSize <= 0 ? 100 : Math.Min(5000, query.PageSize);

        return await companyQueryService.ListAsync(restrictToCompanyIds, query.Search, includeInactive, statusFilter, page, pageSize, cancellationToken);
    }
}
