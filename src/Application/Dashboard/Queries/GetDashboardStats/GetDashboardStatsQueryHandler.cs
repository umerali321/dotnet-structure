using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Dashboard.Dtos;
using SkillsetsBackend.Application.Dashboard.Interfaces;

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
        var companyIds = await DashboardAuthorization.ResolveCompanyScopeAsync(caller, query.CompanyId, _userDirectory, cancellationToken);
        return await _queryService.GetStatsAsync(companyIds, query.StartDate, query.EndDate, cancellationToken);
    }
}
