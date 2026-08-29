using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.LearningTranscript.DTOs;
using SkillsetsBackend.Application.LearningTranscript.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.LearningTranscript.Queries.ListLearningTranscript;

public class ListLearningTranscriptQueryHandler
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 5000;

    private readonly ILearningTranscriptQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public ListLearningTranscriptQueryHandler(
        ILearningTranscriptQueryService queryService,
        IUserDirectory userDirectory,
        IPermissionService permissionService)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<PaginatedList<LearningTranscriptListItemDto>> Handle(ListLearningTranscriptQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        // A caller only viewing their own record (Student/Employee) needs just the base View
        // permission; anyone requesting the multi-employee report needs ViewReport too.
        var requiredPermission = caller.Role == Roles.Student ? Permissions.LearningTranscript.View : Permissions.LearningTranscript.ViewReport;

        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, requiredPermission, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view the learning transcript report.");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        var (restrictToCompanyIds, restrictToManagerId, restrictToUserId) =
            await LearningTranscriptAuthorization.ResolveScopeAsync(caller, _userDirectory, query.CompanyId, cancellationToken);

        var options = new LearningTranscriptQueryOptions(
            page,
            pageSize,
            query.Search,
            query.AssetId,
            query.CompletionStatus,
            query.DateFrom,
            query.DateTo,
            restrictToCompanyIds,
            restrictToManagerId,
            restrictToUserId);

        return await _queryService.ListAsync(options, cancellationToken);
    }
}
