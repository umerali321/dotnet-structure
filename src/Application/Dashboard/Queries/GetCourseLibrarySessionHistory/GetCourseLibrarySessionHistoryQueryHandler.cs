using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Dashboard.Dtos;
using SkillsetsBackend.Application.Dashboard.Interfaces;

namespace SkillsetsBackend.Application.Dashboard.Queries.GetCourseLibrarySessionHistory;

public class GetCourseLibrarySessionHistoryQueryHandler
{
    private readonly IDashboardQueryService _queryService;
    private readonly IUserDirectory _userDirectory;

    public GetCourseLibrarySessionHistoryQueryHandler(IDashboardQueryService queryService, IUserDirectory userDirectory)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
    }

    public async Task<IReadOnlyList<CourseLibrarySessionDto>> Handle(GetCourseLibrarySessionHistoryQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        var companyIds = await DashboardAuthorization.ResolveCompanyScopeAsync(caller, query.CompanyId, _userDirectory, cancellationToken);
        return await _queryService.GetSessionHistoryAsync(query.Email, companyIds, cancellationToken);
    }
}
