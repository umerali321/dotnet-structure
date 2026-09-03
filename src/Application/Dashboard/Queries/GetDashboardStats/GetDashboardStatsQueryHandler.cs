using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Dashboard.Dtos;
using SkillsetsBackend.Application.Dashboard.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsQueryHandler
{
    private readonly IDashboardQueryService _queryService;
    private readonly IUserDirectory _userDirectory;

    public GetDashboardStatsQueryHandler(IDashboardQueryService queryService, IUserDirectory userDirectory)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        var companyIds = await DashboardAuthorization.ResolveCompanyScopeAsync(
            caller, query.CompanyId, _userDirectory, cancellationToken, allowManager: true);

        // Deliberately company-wide for a Manager too, same as CompanyAdmin - not narrowed to just
        // their own team via StudentProfiles.ManagerId. That per-manager scoping was tried first, but
        // the Manager/Employee relation wasn't reliably populated across the existing data (many
        // employees had no ManagerId at all), so it silently under-counted. A Manager's dashboard
        // showing their company's real numbers beats a technically-narrower view that happens to be
        // wrong for most of the data.
        return await _queryService.GetStatsAsync(companyIds, query.StartDate, query.EndDate, restrictToManagerId: null, cancellationToken);
    }
}
