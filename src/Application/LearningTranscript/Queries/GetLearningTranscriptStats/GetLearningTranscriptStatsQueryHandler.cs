using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.LearningTranscript.DTOs;
using SkillsetsBackend.Application.LearningTranscript.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.LearningTranscript.Queries.GetLearningTranscriptStats;

public class GetLearningTranscriptStatsQueryHandler
{
    private readonly ILearningTranscriptQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public GetLearningTranscriptStatsQueryHandler(
        ILearningTranscriptQueryService queryService,
        IUserDirectory userDirectory,
        IPermissionService permissionService)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<LearningTranscriptStatsDto> Handle(GetLearningTranscriptStatsQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        var requiredPermission = caller.Role == Roles.Student ? Permissions.LearningTranscript.View : Permissions.LearningTranscript.ViewReport;

        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, requiredPermission, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view the learning transcript report.");
        }

        var (restrictToCompanyIds, restrictToManagerId, restrictToUserId) =
            await LearningTranscriptAuthorization.ResolveScopeAsync(caller, _userDirectory, query.CompanyId, cancellationToken);

        var options = new LearningTranscriptQueryOptions(
            1,
            1,
            null,
            null,
            null,
            query.DateFrom,
            query.DateTo,
            restrictToCompanyIds,
            restrictToManagerId,
            restrictToUserId);

        return await _queryService.GetStatsAsync(options, cancellationToken);
    }
}
