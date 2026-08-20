using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Dashboard.Dtos;
using SkillsetsBackend.Application.Dashboard.Interfaces;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Dashboard.Queries.GetCourseLibraryUsers;

public class GetCourseLibraryUsersQueryHandler
{
    private readonly IDashboardQueryService _queryService;
    private readonly IUserDirectory _userDirectory;

    public GetCourseLibraryUsersQueryHandler(IDashboardQueryService queryService, IUserDirectory userDirectory)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
    }

    public async Task<PaginatedList<CourseLibraryUserDto>> Handle(GetCourseLibraryUsersQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        var companyIds = await DashboardAuthorization.ResolveCompanyScopeAsync(caller, query.CompanyId, _userDirectory, cancellationToken);
        return await _queryService.GetCourseLibraryUsersAsync(
            companyIds, query.StartDate, query.EndDate, query.Search, query.Page, query.PageSize, cancellationToken);
    }
}
