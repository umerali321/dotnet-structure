using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Managers.DTOs;
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.SystemAdmins.Queries.ListSystemAdmins;

/// <summary>Reuses the Managers query service - a SystemAdmin is the same Users/UserCompanyRoles
/// shape, just a different RoleName - rather than duplicating the paging/search/sort logic.</summary>
public class ListSystemAdminsQueryHandler(IManagerQueryService queryService)
{
    public Task<PaginatedList<ManagerListItemDto>> Handle(
        int page, int pageSize, SearchCriteria? search, bool? active, CallerContext caller, CancellationToken cancellationToken)
    {
        SystemAdminAuthorization.EnsureSuperAdmin(caller);

        return queryService.ListAsync(
            new ManagerListQueryOptions(
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 50 : pageSize,
                search,
                active,
                SortBy: null,
                SortDescending: false,
                // Never narrowed by company: a SystemAdmin's company is only a carrier for the role.
                RestrictToCompanyIds: null,
                RoleFilter: Roles.SystemAdmin),
            cancellationToken);
    }
}
