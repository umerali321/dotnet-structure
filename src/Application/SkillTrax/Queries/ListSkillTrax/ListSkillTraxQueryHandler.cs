using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.SkillTrax.DTOs;
using SkillsetsBackend.Application.SkillTrax.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.SkillTrax.Queries.ListSkillTrax;

public class ListSkillTraxQueryHandler
{
    private readonly ISkillTraxQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public ListSkillTraxQueryHandler(ISkillTraxQueryService queryService, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyList<SkillTraxSummaryDto>> Handle(CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.SkillTrax.View, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view SkillTrax.");
        }

        if (caller.IsPlatformAdmin)
        {
            return await _queryService.ListAsync(null, cancellationToken);
        }

        var managedCompanyIds = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);
        return await _queryService.ListAsync(managedCompanyIds, cancellationToken);
    }
}
