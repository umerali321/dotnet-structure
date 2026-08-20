using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.DTOs;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Students.Queries.ListStudents;

public class ListStudentsQueryHandler
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    private readonly IStudentQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public ListStudentsQueryHandler(IStudentQueryService queryService, IUserDirectory userDirectory, IPermissionService permissionService)
    {
        _queryService = queryService;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<PaginatedList<StudentListItemDto>> Handle(ListStudentsQuery query, CallerContext caller, CancellationToken cancellationToken)
    {
        // Permission-driven (RolePermissions), not a hardcoded role check - see
        // ListManagersQueryHandler for the identical pattern.
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Students.View, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view employees.");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        IReadOnlyCollection<int>? restrictToCompanyIds;

        if (caller.IsSuperAdmin)
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

        var restrictToManagerId = caller.Role == Roles.Manager ? caller.DbUserId : null;

        var options = new StudentListQueryOptions(
            page,
            pageSize,
            query.Search,
            query.StudentType,
            query.IsActive,
            query.SortBy,
            query.SortDescending,
            restrictToCompanyIds,
            restrictToManagerId);

        return await _queryService.ListAsync(options, cancellationToken);
    }
}
