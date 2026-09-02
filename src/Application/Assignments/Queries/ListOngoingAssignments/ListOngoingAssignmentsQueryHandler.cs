using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Assignments.Queries.ListOngoingAssignments;

public class ListOngoingAssignmentsQueryHandler
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    private readonly IAssignmentQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public ListOngoingAssignmentsQueryHandler(IAssignmentQueryService queryService, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<PaginatedList<AssignmentDto>> Handle(ListOngoingAssignmentsQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Assignments.View, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view training assignments.");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        IReadOnlyCollection<int>? restrictToCompanyIds;

        if (caller.IsPlatformAdmin)
        {
            restrictToCompanyIds = query.CompanyId.HasValue ? [query.CompanyId.Value] : null;
        }
        else
        {
            var managed = await StudentAuthorization.GetManagedCompanyIdsAsync(caller, _userDirectory, cancellationToken);

            if (query.CompanyId.HasValue)
            {
                if (!managed.Contains(query.CompanyId.Value))
                {
                    throw new UnauthorizedAccessException("You do not have access to that company.");
                }

                restrictToCompanyIds = [query.CompanyId.Value];
            }
            else
            {
                restrictToCompanyIds = managed;
            }
        }

        return await _queryService.ListManagedAsync(restrictToCompanyIds, page, pageSize, cancellationToken);
    }
}
